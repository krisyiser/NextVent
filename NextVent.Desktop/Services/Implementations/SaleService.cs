using Microsoft.EntityFrameworkCore;
using NextVent.Core.Helpers;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using NextVent.Core.Models;
using NextVent.Core.Enums;
using System.Text.Json;

namespace NextVent.Services.Implementations;

/// <summary>
/// Sale transaction service with atomic stock deduction and debt management.
/// Critical transactional rules migrated from storage.ts saveSale/cancelSale.
/// </summary>
public sealed class SaleService : ISaleService
{
    private readonly AppDbContext _ctx;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SaleService(AppDbContext ctx) => _ctx = ctx;

    /// <summary>
    /// Atomic sale save: INSERT sale + UPDATE stock + UPDATE debt (if credit) + UPDATE co-occurrences.
    /// Uses EF Core transaction to guarantee all-or-nothing.
    /// </summary>
    public async Task<SaleDto> SaveAsync(SaleDto sale)
    {
        if (sale.Items is null || sale.Items.Count == 0)
        {
            throw new InvalidOperationException("No se puede registrar una venta sin productos.");
        }

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var saleId = string.IsNullOrEmpty(sale.Id) ? IdGenerator.NewSaleId() : sale.Id;

            // Global discount proration & VAT (IVA) calculation
            double snapshotsSum = sale.Items.Sum(i => (i.Quantity * (i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice)) - i.AppliedDiscountAmount);
            double globalDiscountAmount = Math.Max(0.0, Math.Round(snapshotsSum - sale.Total, 2));
            var processedItems = ApplyProratedGlobalDiscountAndTaxes(sale.Items, globalDiscountAmount);

            var itemsJson = JsonSerializer.Serialize(
                processedItems,
                NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListSaleItemSnapshotDto);

            var total = Math.Round(sale.Total, 2);
            var totalCost = Math.Round(sale.TotalCost, 2);
            var profit = Math.Round(sale.Profit, 2);
            var paidAmount = Math.Round(sale.PaidAmount, 2);
            var changeAmount = Math.Round(sale.ChangeAmount, 2);

            var entity = new SaleEntity
            {
                Id = saleId,
                Date = string.IsNullOrEmpty(sale.Date) ? DateTimeOffset.UtcNow.ToString("o") : sale.Date,
                ItemsJson = itemsJson,
                Total = total,
                TotalCost = totalCost,
                Profit = profit,
                PaidAmount = paidAmount,
                ChangeAmount = changeAmount,
                PaymentMethod = sale.PaymentMethod,
                CustomerId = sale.CustomerId,
                IsCredit = sale.IsCredit ? 1 : 0,
                IsCancelled = 0,
                EstadoFiscal = sale.EstadoFiscal
            };

            _ctx.Sales.Add(entity);

            // Deduct inventory recursively (Combo/Kit support) & compute true COGS
            double totalTicketCogs = 0.0;
            foreach (var item in processedItems)
            {
                double unitCogs = await DeductInventoryRecursiveAsync(item.ProductId, item.Quantity, new System.Collections.Generic.HashSet<string>());
                totalTicketCogs += unitCogs * item.Quantity;
            }

            totalCost = Math.Round(totalTicketCogs, 2);
            profit = Math.Round(total - totalCost, 2);
            entity.TotalCost = totalCost;
            entity.Profit = profit;

            // If customer linked, validate credit constraints & update debt or accrue/deduct loyalty points
            if (!string.IsNullOrEmpty(sale.CustomerId))
            {
                var customer = await _ctx.Customers.FindAsync(sale.CustomerId);
                if (customer is not null)
                {
                    if (sale.IsCredit)
                    {
                        if (customer.IsCreditBlocked)
                        {
                            throw new InvalidOperationException($"El crédito está bloqueado para el cliente: {customer.Name}");
                        }

                        decimal creditRequired = (decimal)total;
                        decimal availableCredit = customer.AvailableCredit;

                        if (availableCredit < creditRequired)
                        {
                            throw new InvalidOperationException($"Crédito insuficiente. Disponible: {availableCredit:C}, Requerido: {creditRequired:C}");
                        }

                        customer.Debt = Math.Round(customer.Debt + total, 2);
                    }

                    if (sale.PaymentMethod == "Puntos de Fidelidad")
                    {
                        // Deduct points used for payment (1 pt = $1.00 MXN)
                        customer.PuntosSaldo = Math.Max(0.0, customer.PuntosSaldo - total);
                    }
                    else
                    {
                        // Accrue 1 point per $10 spent
                        double earnedPoints = Math.Floor(total / 10.0);
                        customer.PuntosSaldo += earnedPoints;
                    }
                }
                else if (sale.IsCredit)
                {
                    throw new InvalidOperationException("Cliente no encontrado en la base de datos.");
                }
            }
            else if (sale.IsCredit)
            {
                throw new InvalidOperationException("Debe asignar un cliente registrado para cobrar a crédito.");
            }

            try
            {
                await _ctx.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw new DbUpdateConcurrencyException("El inventario cambió durante la transacción. Intente cobrar de nuevo.");
            }

            return sale with
            {
                Id = saleId,
                Items = processedItems,
                Total = total,
                TotalCost = totalCost,
                Profit = profit,
                PaidAmount = paidAmount,
                ChangeAmount = changeAmount
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SaleResultModel> ProcessSaleAsync(SaleCreationDto dto)
    {
        bool isCredit = dto.CreditAmount > 0 || dto.PaymentMethod == PaymentMethod.Credito || dto.PaymentMethod == PaymentMethod.Credit;
        var snapshots = dto.Items.Select(i => new SaleItemSnapshotDto(
            ProductId: i.Id,
            Name: i.Name,
            UnitPrice: i.UnitPrice,
            Cost: i.UnitPrice * 0.6,
            Quantity: i.Quantity,
            Unit: i.Unit,
            Category: i.Category ?? "General",
            Discount: i.AppliedDiscountAmount,
            TotalPrice: i.TotalPrice,
            OriginalUnitPrice: i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice,
            AppliedDiscountAmount: i.AppliedDiscountAmount,
            AppliedPromotionId: i.AppliedPromotionId
        )).ToList();

        var totalCost = snapshots.Sum(s => s.Cost * s.Quantity);
        var profit = dto.Total - totalCost;

        var saleDto = new SaleDto(
            Id: IdGenerator.NewSaleId(),
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: snapshots,
            Total: dto.Total,
            TotalCost: totalCost,
            Profit: profit,
            PaidAmount: dto.Total,
            ChangeAmount: 0,
            PaymentMethod: dto.PaymentMethod.ToString(),
            CustomerId: dto.CustomerId,
            IsCredit: isCredit,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        try
        {
            var saved = await SaveAsync(saleDto);
            return new SaleResultModel { IsSuccess = true, SaleId = saved.Id };
        }
        catch (Exception ex)
        {
            return new SaleResultModel { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<List<SaleDto>> GetHistoryAsync(int limit = 500)
    {
        var entities = await _ctx.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.Date)
            .Take(limit)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<SaleDto>> GetSalesByDateRangeAsync(DateTime start, DateTime end)
    {
        string startStr = start.ToString("o");
        string endStr = end.ToString("o");

        var entities = await _ctx.Sales
            .AsNoTracking()
            .Where(s => string.Compare(s.Date, startStr) >= 0 && string.Compare(s.Date, endStr) <= 0)
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Atomic cancellation: restore stock + reduce debt + mark cancelled.
    /// </summary>
    public async Task CancelAsync(string saleId)
    {
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var sale = await _ctx.Sales.FindAsync(saleId);
            if (sale is null || sale.IsCancelled == 1) return;

            var items = JsonSerializer.Deserialize(
                sale.ItemsJson,
                NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListSaleItemSnapshotDto) ?? [];

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
            sale.CancelledAt = DateTimeOffset.UtcNow.ToString("o");

            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateFiscalStatusAsync(string saleId, string status, string? uuid, string? folio)
    {
        var sale = await _ctx.Sales.FindAsync(saleId);
        if (sale is null) return;

        sale.EstadoFiscal = status;
        sale.UuidSat = uuid;
        sale.SerieFolio = folio;
        await _ctx.SaveChangesAsync();
    }

    public async Task<bool> ProcessPartialReturnAsync(string saleId, string productId, double returnQty, string reason, string refundMethod = "Efectivo", bool isProductInGoodCondition = true)
    {
        if (returnQty <= 0) return false;
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var sale = await _ctx.Sales.FindAsync(saleId);
            if (sale is null || sale.IsCancelled == 1) return false;

            var items = JsonSerializer.Deserialize(
                sale.ItemsJson,
                NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListSaleItemSnapshotDto) ?? [];
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
                await RestockInventoryRecursiveAsync(targetItem.ProductId, returnQty, new System.Collections.Generic.HashSet<string>());
            }
            else
            {
                // DO NOT restock. Write to AuditLog as Merma (Shrinkage).
                var product = await _ctx.Products.FindAsync(targetItem.ProductId);
                if (product != null)
                {
                    _ctx.AuditLogs.Add(new AuditLogEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        ActionType = AuditActionType.InventoryStockAdjustment,
                        UserId = string.Empty,
                        Reason = $"Devolución de producto dañado/mermado: {reason}",
                        FinancialImpact = product.Cost * returnQty,
                        EntityName = "products",
                        EntityId = targetItem.ProductId
                    });
                }
            }

            // 2. Adjust item quantity and sale totals
            double refundedAmount = Math.Round(targetItem.UnitPrice * returnQty, 2);
            double refundedCost = Math.Round(targetItem.Cost * returnQty, 2);

            sale.Total = Math.Max(0.0, Math.Round(sale.Total - refundedAmount, 2));
            sale.TotalCost = Math.Max(0.0, Math.Round(sale.TotalCost - refundedCost, 2));
            sale.Profit = Math.Max(0.0, Math.Round(sale.Profit - (refundedAmount - refundedCost), 2));

            var updatedItems = new List<SaleItemSnapshotDto>();
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
                sale.CancelledAt = DateTimeOffset.UtcNow.ToString("o");
            }

            sale.ItemsJson = JsonSerializer.Serialize(
                updatedItems,
                NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListSaleItemSnapshotDto);

            // Record Return Audit Entity
            var returnEntity = new ReturnEntity
            {
                Id = Guid.NewGuid().ToString(),
                OriginalSaleId = sale.Id,
                CashierUserId = null,
                TotalRefunded = refundedAmount,
                CogsReversed = refundedCost,
                RefundMethod = refundMethod,
                Reason = reason,
                CreatedAt = DateTimeOffset.UtcNow.ToString("o")
            };
            _ctx.Returns.Add(returnEntity);

            // Inject Physical Cash Outflow to Active Shift Drawer if Refunded in Cash
            var activeShift = await _ctx.Shifts.FirstOrDefaultAsync(s => s.IsOpen == 1);
            if (activeShift != null && (refundMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) || refundMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)))
            {
                _ctx.ShiftMovements.Add(new ShiftMovementEntity
                {
                    ShiftId = activeShift.Id,
                    MovementType = NextVent.Core.Enums.MovementType.DevolucionCliente,
                    Amount = refundedAmount,
                    IsOutflow = true,
                    Description = $"Devolución Ticket #{sale.Id} - {reason}",
                    ReferenceId = returnEntity.Id,
                    Timestamp = DateTime.UtcNow.ToString("s")
                });
            }

            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            Serilog.Log.Information("Processed Partial Return for Sale {SaleId}, Product {ProductId}, Qty {Qty}, Refund {Amount}",
                saleId, productId, returnQty, refundedAmount);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Serilog.Log.Error(ex, "Error processing partial return for sale {SaleId}", saleId);
            throw;
        }
    }

    public async Task<List<CashierPerformanceDto>> GetCashierPerformanceReportAsync(double defaultCommissionPct = 3.0)
    {
        try
        {
            var validSales = await _ctx.Sales
                .AsNoTracking()
                .Where(s => s.IsCancelled == 0)
                .ToListAsync();

            var users = await _ctx.Users
                .AsNoTracking()
                .ToListAsync();

            // Group sales by user or cashier name
            var report = new List<CashierPerformanceDto>();

            if (users.Count > 0)
            {
                foreach (var user in users)
                {
                    var userSales = validSales; // Currently system active sales
                    int count = userSales.Count;
                    double totalRev = userSales.Sum(s => s.Total);
                    double avgTicket = count > 0 ? totalRev / count : 0.0;
                    double commission = totalRev * (defaultCommissionPct / 100.0);

                    report.Add(new CashierPerformanceDto(
                        user.FullName,
                        count,
                        Math.Round(totalRev, 2),
                        Math.Round(avgTicket, 2),
                        defaultCommissionPct,
                        Math.Round(commission, 2)
                    ));
                }
            }
            else
            {
                int count = validSales.Count;
                double totalRev = validSales.Sum(s => s.Total);
                double avgTicket = count > 0 ? totalRev / count : 0.0;
                double commission = totalRev * (defaultCommissionPct / 100.0);

                report.Add(new CashierPerformanceDto(
                    "CAJERO MATRIZ",
                    count,
                    Math.Round(totalRev, 2),
                    Math.Round(avgTicket, 2),
                    defaultCommissionPct,
                    Math.Round(commission, 2)
                ));
            }

            return report;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error generating cashier performance report");
            return [];
        }
    }

    private async Task CheckAndFlagLowStockAsync(ProductEntity product)
    {
        if (product.Stock <= product.MinStock)
        {
            bool alertExists = await _ctx.SystemAlerts
                .AnyAsync(a => a.ProductId == product.Id && !a.IsResolved);

            if (!alertExists)
            {
                var alert = new SystemAlertEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductId = product.Id,
                    SupplierId = product.DefaultSupplierId,
                    Title = $"Stock Crítico: {product.Name}",
                    Message = $"Stock actual ({product.Stock}) por debajo del mínimo permitido ({product.MinStock}).",
                    CreatedAt = DateTime.UtcNow.ToString("s"),
                    IsResolved = false
                };
                _ctx.SystemAlerts.Add(alert);
            }
        }
    }

    private async Task<double> DeductInventoryRecursiveAsync(string productId, double quantityMultiplier, System.Collections.Generic.HashSet<string> executionStack)
    {
        var product = await _ctx.Products.FindAsync(productId)
            ?? throw new InvalidOperationException($"Producto o ingrediente no encontrado: {productId}");

        if (product.IsKit)
        {
            if (!executionStack.Add(productId))
                throw new InvalidOperationException($"Error crítico: Referencia circular detectada en el Combo/Kit: {product.Name}");

            var kit = await _ctx.ItemKits
                .Include(k => k.Components)
                .FirstOrDefaultAsync(k => k.ParentProductId == product.Id || k.Id == product.Id)
                ?? throw new InvalidOperationException($"Estructura de Combo no encontrada para: {product.Name}");

            double totalKitUnitCost = 0.0;

            foreach (var component in kit.Components)
            {
                double totalRequired = quantityMultiplier * component.Quantity;
                double compUnitCost = await DeductInventoryRecursiveAsync(component.ProductId, totalRequired, executionStack);
                totalKitUnitCost += component.Quantity * compUnitCost;
            }

            executionStack.Remove(productId);
            return totalKitUnitCost;
        }
        else
        {
            product.Stock = Math.Max(0.0, Math.Round(product.Stock - quantityMultiplier, 3));
            _ctx.Products.Update(product);
            await CheckAndFlagLowStockAsync(product);
            return product.Cost;
        }
    }

    private async Task RestockInventoryRecursiveAsync(string productId, double quantityMultiplier, System.Collections.Generic.HashSet<string> executionStack)
    {
        var product = await _ctx.Products.FindAsync(productId)
            ?? throw new InvalidOperationException($"Producto o ingrediente no encontrado: {productId}");

        if (product.IsKit)
        {
            if (!executionStack.Add(productId))
                throw new InvalidOperationException($"Error crítico: Referencia circular detectada en el Combo/Kit: {product.Name}");

            var kit = await _ctx.ItemKits
                .Include(k => k.Components)
                .FirstOrDefaultAsync(k => k.ParentProductId == product.Id || k.Id == product.Id)
                ?? throw new InvalidOperationException($"Estructura de Combo no encontrada para: {product.Name}");

            foreach (var component in kit.Components)
            {
                double totalRequired = quantityMultiplier * component.Quantity;
                await RestockInventoryRecursiveAsync(component.ProductId, totalRequired, executionStack);
            }

            executionStack.Remove(productId);
        }
        else
        {
            product.Stock = Math.Round(product.Stock + quantityMultiplier, 3);
            _ctx.Products.Update(product);
        }
    }

    private static SaleDto MapToDto(SaleEntity e)
    {
        var items = JsonSerializer.Deserialize(
            e.ItemsJson,
            NextVent.Desktop.Core.Helpers.NextVentJsonContext.Default.ListSaleItemSnapshotDto) ?? [];
        return new SaleDto(
            e.Id, e.Date, items, e.Total, e.TotalCost, e.Profit,
            e.PaidAmount, e.ChangeAmount, e.PaymentMethod,
            e.CustomerId, e.IsCredit == 1, e.IsCancelled == 1,
            e.CancelledAt, e.EstadoFiscal, e.UuidSat, e.SerieFolio);
    }

    private List<SaleItemSnapshotDto> ApplyProratedGlobalDiscountAndTaxes(List<SaleItemSnapshotDto> items, double globalDiscountAmount)
    {
        decimal totalCartSubtotal = (decimal)items.Sum(i => (i.Quantity * (i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice)) - i.AppliedDiscountAmount);
        decimal globalDiscountDec = (decimal)globalDiscountAmount;
        decimal accumulatedProratedDiscount = 0m;
        var result = new List<SaleItemSnapshotDto>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            double itemUnitPrice = item.OriginalUnitPrice > 0 ? item.OriginalUnitPrice : item.UnitPrice;
            decimal lineSubtotal = (decimal)((item.Quantity * itemUnitPrice) - item.AppliedDiscountAmount);
            decimal proratedGlobalDiscount = 0m;

            if (totalCartSubtotal > 0 && globalDiscountDec > 0)
            {
                if (i == items.Count - 1)
                {
                    proratedGlobalDiscount = globalDiscountDec - accumulatedProratedDiscount;
                }
                else
                {
                    decimal weight = lineSubtotal / totalCartSubtotal;
                    proratedGlobalDiscount = Math.Round(globalDiscountDec * weight, 2);
                    accumulatedProratedDiscount += proratedGlobalDiscount;
                }
            }

            // TAX CALCULATION (16% IVA) MUST OCCUR AFTER ALL DISCOUNTS
            decimal finalTaxableBase = lineSubtotal - proratedGlobalDiscount;
            decimal taxAmount = Math.Round(finalTaxableBase * (decimal)NextVent.Core.Constants.AppConstants.DefaultIvaRate, 2);
            decimal totalLineAmount = Math.Round(finalTaxableBase + taxAmount, 2);

            result.Add(item with
            {
                ProratedGlobalDiscountAmount = (double)proratedGlobalDiscount,
                TaxAmount = (double)taxAmount,
                TotalPrice = (double)totalLineAmount
            });
        }
        return result;
    }
}
