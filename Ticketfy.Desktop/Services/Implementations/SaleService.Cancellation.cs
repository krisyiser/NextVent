using Microsoft.EntityFrameworkCore;
using Ticketfy.Core.Enums;
using Ticketfy.Core.Helpers;
using Ticketfy.Data;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ticketfy.Services.Implementations;

/// <summary>
/// Partial extension of SaleService handling cancellation, partial returns, and fiscal status updates.
/// </summary>
public partial class SaleService
{
    /// <summary>
    /// Atomic cancellation: restore stock + reduce debt + mark cancelled.
    /// </summary>
    public async Task CancelAsync(string saleId)
    {
        await CancelSaleAsync(saleId, "Cancelación Administrativa");
    }

    public async Task<bool> CancelSaleAsync(string saleId, string reason)
    {
        using var _ctx = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var sale = await _ctx.Sales.FindAsync(saleId);
            if (sale is null || sale.IsCancelled == 1 || sale.Status == SaleStatus.Canceled)
                return false;

            var items = JsonSerializer.Deserialize(
                sale.ItemsJson,
                Ticketfy.Desktop.Core.Helpers.TicketfyJsonContext.Default.ListSaleItemSnapshotDto) ?? [];

            // Restore stock
            foreach (var item in items)
            {
                var product = await _ctx.Products.FindAsync(item.Id);
                if (product is not null)
                {
                    double remaining = Math.Max(0.0, item.Quantity - item.ReturnedQuantity);
                    product.Stock = Math.Round(product.Stock + remaining, 3);
                }
            }

            // If was credit, reduce customer debt
            if (sale.IsCredit == 1 && !string.IsNullOrEmpty(sale.CustomerId))
            {
                var customer = await _ctx.Customers.FindAsync(sale.CustomerId);
                if (customer is not null)
                {
                    customer.Debt = Math.Max(0.0, Math.Round(customer.Debt - sale.Total, 2));
                }
            }

            sale.IsCancelled = 1;
            sale.CancelledAt = DateTimeOffset.Now.ToString("o");
            sale.Status = SaleStatus.Canceled;
            sale.CancellationReason = reason;
            sale.CancellationDate = DateTimeOffset.Now.ToString("o");

            // Inject Physical Cash Outflow to Active Shift Drawer if Cancelled Sale was paid in Cash
            var activeShift = await _ctx.Shifts.FirstOrDefaultAsync(s => s.IsOpen == 1);
            if (activeShift != null && (sale.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) || sale.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)))
            {
                _ctx.ShiftMovements.Add(new ShiftMovementEntity
                {
                    ShiftId = activeShift.Id,
                    MovementType = Ticketfy.Core.Enums.MovementType.DevolucionCliente,
                    Amount = sale.Total,
                    IsOutflow = true,
                    Description = $"Cancelación Venta Ticket #{sale.Id} - {reason}",
                    ReferenceId = sale.Id,
                    Timestamp = DateTime.Now.ToString("s")
                });
            }

            await DbResilienceHelper.ExecuteWithRetryAsync(async () =>
            {
                await _ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateFiscalStatusAsync(string saleId, string status, string? uuid, string? folio)
    {
        using var _ctx = await _contextFactory.CreateDbContextAsync();
        var sale = await _ctx.Sales.FindAsync(saleId);
        if (sale is null) return;

        sale.EstadoFiscal = status;
        sale.UuidSat = uuid;
        sale.SerieFolio = folio;
        await DbResilienceHelper.ExecuteWithRetryAsync(async () => await _ctx.SaveChangesAsync());
    }

    public async Task<bool> ProcessPartialReturnAsync(string saleId, string productId, double returnQty, string reason, string refundMethod = "Efectivo", bool isProductInGoodCondition = true)
    {
        if (returnQty <= 0) return false;
        using var _ctx = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var sale = await _ctx.Sales.FindAsync(saleId);
            if (sale is null || sale.IsCancelled == 1) return false;

            var items = JsonSerializer.Deserialize(
                sale.ItemsJson,
                Ticketfy.Desktop.Core.Helpers.TicketfyJsonContext.Default.ListSaleItemSnapshotDto) ?? [];
            var targetItem = items.FirstOrDefault(i => i.ProductId == productId || i.Id == productId);

            if (targetItem is null) return false;

            // ANTI-FRAUD CHECK: Prevent returning more than available quantity
            if (returnQty > targetItem.AvailableForReturn)
            {
                throw new InvalidOperationException($"Fraude Detectado: Intento de devolver más unidades de las disponibles para el ítem {targetItem.ProductId}");
            }

            // 1. Restock product stock recursively in database (only if product is in good condition)
            if (isProductInGoodCondition)
            {
                await RestockInventoryRecursiveAsync(_ctx, targetItem.ProductId, returnQty, new HashSet<string>());
            }
            else
            {
                // DO NOT restock. Write to AuditLog as Merma (Shrinkage).
                var product = await _ctx.Products.FindAsync(targetItem.ProductId);
                if (product != null)
                {
                    await _auditService.LogAsync(new AuditLogEntity
                    {
                        ActionType = Ticketfy.Core.Enums.AuditActionType.InventoryStockAdjustment,
                        RiskLevel = Ticketfy.Core.Enums.RiskLevel.Warning,
                        UserId = "SYSTEM",
                        EntityName = "products",
                        EntityId = targetItem.ProductId,
                        FinancialImpact = product.Cost * returnQty,
                        Reason = $"Devolución de producto dañado/mermado: {reason}"
                    });
                }
            }

            // 2. Adjust item quantity and sale totals
            double refundedAmount = Math.Round(targetItem.UnitPrice * returnQty, 2);
            double refundedCost = Math.Round(targetItem.Cost * returnQty, 2);

            sale.Total = Math.Max(0.0, Math.Round(sale.Total - refundedAmount, 2));
            sale.TotalCost = Math.Max(0.0, Math.Round(sale.TotalCost - refundedCost, 2));
            sale.Profit = Math.Max(0.0, Math.Round(sale.Profit - (refundedAmount - refundedCost), 2));

            var updatedItems = new List<Ticketfy.Data.Dtos.SaleItemSnapshotDto>();
            foreach (var item in items)
            {
                if (item.ProductId == productId || item.Id == productId)
                {
                    double newReturnedQty = item.ReturnedQuantity + returnQty;
                    updatedItems.Add(item with { ReturnedQuantity = newReturnedQty });
                }
                else
                {
                    updatedItems.Add(item);
                }
            }

            // Check if all items on the ticket are fully returned
            bool allReturned = updatedItems.All(i => i.ReturnedQuantity >= i.Quantity);
            if (allReturned)
            {
                sale.IsCancelled = 1;
                sale.CancelledAt = DateTimeOffset.Now.ToString("o");
                sale.Status = SaleStatus.Refunded;
            }
            else
            {
                sale.Status = SaleStatus.Refunded;
            }

            sale.ItemsJson = JsonSerializer.Serialize(
                updatedItems,
                Ticketfy.Desktop.Core.Helpers.TicketfyJsonContext.Default.ListSaleItemSnapshotDto);

            // Record Return Audit Entity
            var returnEntity = new ReturnEntity
            {
                Id = await GenerateNextReturnFolioAsync(_ctx),
                OriginalSaleId = sale.Id,
                CashierUserId = null,
                TotalRefunded = refundedAmount,
                CogsReversed = refundedCost,
                RefundMethod = refundMethod,
                Reason = reason,
                CreatedAt = DateTimeOffset.Now.ToString("o")
            };
            _ctx.Returns.Add(returnEntity);

            // Inject Physical Cash Outflow to Active Shift Drawer if Refunded in Cash
            var activeShift = await _ctx.Shifts.FirstOrDefaultAsync(s => s.IsOpen == 1);
            if (activeShift != null && (refundMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) || refundMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)))
            {
                _ctx.ShiftMovements.Add(new ShiftMovementEntity
                {
                    ShiftId = activeShift.Id,
                    MovementType = Ticketfy.Core.Enums.MovementType.DevolucionCliente,
                    Amount = refundedAmount,
                    IsOutflow = true,
                    Description = $"Devolución Ticket #{sale.Id} - {reason}",
                    ReferenceId = returnEntity.Id,
                    Timestamp = DateTime.Now.ToString("s")
                });
            }

            await DbResilienceHelper.ExecuteWithRetryAsync(async () =>
            {
                await _ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            });

            Log.Information("Processed Partial Return for Sale {SaleId}, Product {ProductId}, Qty {Qty}, Refund {Amount}",
                saleId, productId, returnQty, refundedAmount);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Error processing partial return for sale {SaleId}", saleId);
            throw;
        }
    }
}
