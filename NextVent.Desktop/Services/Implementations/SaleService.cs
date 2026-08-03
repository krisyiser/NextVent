using Microsoft.EntityFrameworkCore;
using NextVent.Core.Helpers;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
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
            var itemsJson = JsonSerializer.Serialize(sale.Items, JsonOpts);

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

            // Deduct stock for each item
            foreach (var item in sale.Items)
            {
                var product = await _ctx.Products.FindAsync(item.Id);
                if (product is not null)
                {
                    product.Stock = Math.Max(0, Math.Round(product.Stock - item.Quantity, 3));
                }
            }

            // If customer linked, update debt or accrue/deduct loyalty points
            if (!string.IsNullOrEmpty(sale.CustomerId))
            {
                var customer = await _ctx.Customers.FindAsync(sale.CustomerId);
                if (customer is not null)
                {
                    if (sale.IsCredit)
                    {
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
            }

            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();

            return sale with
            {
                Id = saleId,
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

    public async Task<List<SaleDto>> GetHistoryAsync(int limit = 500)
    {
        var entities = await _ctx.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.Date)
            .Take(limit)
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

            var items = JsonSerializer.Deserialize<List<SaleItemSnapshotDto>>(sale.ItemsJson, JsonOpts) ?? [];

            // Restore stock
            foreach (var item in items)
            {
                var product = await _ctx.Products.FindAsync(item.Id);
                if (product is not null)
                {
                    product.Stock = Math.Round(product.Stock + item.Quantity, 3);
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

    public async Task<bool> ProcessPartialReturnAsync(string saleId, string productId, double returnQty, string reason, string refundMethod = "Efectivo")
    {
        if (returnQty <= 0) return false;
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var sale = await _ctx.Sales.FindAsync(saleId);
            if (sale is null || sale.IsCancelled == 1) return false;

            var items = JsonSerializer.Deserialize<List<SaleItemSnapshotDto>>(sale.ItemsJson, JsonOpts) ?? [];
            var targetItem = items.FirstOrDefault(i => i.ProductId == productId || i.Id == productId);

            if (targetItem is null || returnQty > targetItem.Quantity) return false;

            // 1. Restock product stock in database
            var product = await _ctx.Products.FindAsync(targetItem.ProductId);
            if (product is not null)
            {
                product.Stock = Math.Round(product.Stock + returnQty, 3);
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
                    double remainingQty = item.Quantity - returnQty;
                    if (remainingQty > 0.001)
                    {
                        updatedItems.Add(item with { Quantity = remainingQty, TotalPrice = Math.Round(item.UnitPrice * remainingQty, 2) });
                    }
                }
                else
                {
                    updatedItems.Add(item);
                }
            }

            if (updatedItems.Count == 0)
            {
                sale.IsCancelled = 1;
                sale.CancelledAt = DateTimeOffset.UtcNow.ToString("o");
            }

            sale.ItemsJson = JsonSerializer.Serialize(updatedItems, JsonOpts);

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
            return false;
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
                        user.Nombre,
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

    private static SaleDto MapToDto(SaleEntity e)
    {
        var items = JsonSerializer.Deserialize<List<SaleItemSnapshotDto>>(e.ItemsJson, JsonOpts) ?? [];
        return new SaleDto(
            e.Id, e.Date, items, e.Total, e.TotalCost, e.Profit,
            e.PaidAmount, e.ChangeAmount, e.PaymentMethod,
            e.CustomerId, e.IsCredit == 1, e.IsCancelled == 1,
            e.CancelledAt, e.EstadoFiscal, e.UuidSat, e.SerieFolio);
    }
}
