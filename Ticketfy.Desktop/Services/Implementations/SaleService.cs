using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ticketfy.Core.Helpers;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using Ticketfy.Core.Models;
using Ticketfy.Core.Enums;
using Serilog;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.Services.Implementations;

/// <summary>
/// Sale transaction service with atomic stock deduction and debt management.
/// Decomposed into partial classes: SaleService (Core), SaleService.Cancellation, SaleService.Inventory.
/// </summary>
public partial class SaleService : ISaleService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly CoOccurrenceQueue _coOccurrenceQueue;
    private readonly Ticketfy.Services.Interfaces.IAuditService _auditService;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SaleService(IDbContextFactory<AppDbContext> contextFactory, CoOccurrenceQueue coOccurrenceQueue, Ticketfy.Services.Interfaces.IAuditService auditService)
    {
        _contextFactory = contextFactory;
        _coOccurrenceQueue = coOccurrenceQueue;
        _auditService = auditService;
    }

    public async Task<SaleDto> SaveAsync(SaleDto sale)
    {
        if (sale.Items is null || sale.Items.Count == 0)
        {
            throw new InvalidOperationException("No se puede registrar una venta sin productos.");
        }

        using var _ctx = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var saleId = sale.Id;
            if (string.IsNullOrEmpty(saleId) || !saleId.Contains("-") || saleId.Length != 11)
            {
                while (true)
                {
                    saleId = await GenerateNextSaleFolioAsync(_ctx);
                    if (!await _ctx.Sales.AnyAsync(s => s.Id == saleId))
                        break;
                }
            }

            // Global discount proration & VAT (IVA) calculation
            double snapshotsSum = sale.Items.Sum(i => (i.Quantity * (i.OriginalUnitPrice > 0 ? i.OriginalUnitPrice : i.UnitPrice)) - i.AppliedDiscountAmount);
            double globalDiscountAmount = Math.Max(0.0, Math.Round(snapshotsSum - sale.Total, 2));
            var processedItems = ApplyProratedGlobalDiscountAndTaxes(sale.Items, globalDiscountAmount);

            var itemsJson = JsonSerializer.Serialize(
                processedItems,
                Ticketfy.Desktop.Core.Helpers.TicketfyJsonContext.Default.ListSaleItemSnapshotDto);

            var total = Math.Round(sale.Total, 2);
            var totalCost = Math.Round(sale.TotalCost, 2);
            var profit = Math.Round(sale.Profit, 2);
            var paidAmount = Math.Round(sale.PaidAmount, 2);
            var changeAmount = Math.Round(sale.ChangeAmount, 2);

            var entity = new SaleEntity
            {
                Id = saleId,
                Date = string.IsNullOrEmpty(sale.Date) ? DateTimeOffset.Now.ToString("o") : sale.Date,
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
                EstadoFiscal = sale.EstadoFiscal,
                SerieFolio = saleId,
                Status = SaleStatus.Completed,
                InvoiceId = sale.InvoiceId,
                InvoiceStatus = sale.InvoiceStatus
            };

            _ctx.Sales.Add(entity);

            // Deduct inventory recursively (Combo/Kit support) & compute true COGS
            double totalTicketCogs = 0.0;
            foreach (var item in processedItems)
            {
                double unitCogs = await DeductInventoryRecursiveAsync(_ctx, item.ProductId, item.Quantity, new HashSet<string>());
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
                        customer.PuntosSaldo = Math.Max(0.0, customer.PuntosSaldo - total);
                    }
                    else
                    {
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
                await DbResilienceHelper.ExecuteWithRetryAsync(async () =>
                {
                    await _ctx.SaveChangesAsync();
                    await transaction.CommitAsync();
                });
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
                ChangeAmount = changeAmount,
                SerieFolio = saleId,
                Status = SaleStatus.Completed
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
            Date: DateTimeOffset.Now.ToString("o"),
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

    public async Task<SaleDto?> GetByIdAsync(string saleId)
    {
        using var _ctx = await _contextFactory.CreateDbContextAsync();
        var entity = await _ctx.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (entity == null) return null;
        return MapToDto(entity);
    }

    public async Task<List<SaleDto>> GetHistoryAsync(int limit = 500)
    {
        using var _ctx = await _contextFactory.CreateDbContextAsync();
        var entities = await _ctx.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.Date)
            .Take(limit)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<SaleDto>> GetSalesByDateRangeAsync(DateTime start, DateTime end)
    {
        using var _ctx = await _contextFactory.CreateDbContextAsync();
        var allEntities = await _ctx.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        var filtered = allEntities
            .Where(s => s.Date.IsInDateRange(start, end))
            .ToList();

        return filtered.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Generates date-bounded cashier performance analytics with SQL-level projection to prevent memory leaks and unbounded data dumps.
    /// </summary>
    public async Task<List<CashierPerformanceDto>> GetCashierPerformanceReportAsync(DateTime? startDate = null, DateTime? endDate = null, double defaultCommissionPct = 0.0)
    {
        try
        {
            using var _ctx = await _contextFactory.CreateDbContextAsync();
            var allSales = await _ctx.Sales
                .AsNoTracking()
                .Where(s => s.IsCancelled == 0)
                .ToListAsync();

            var validSales = allSales
                .Where(s => s.Date.IsInDateRange(startDate, endDate))
                .ToList();
            var users = await _ctx.Users.AsNoTracking().ToListAsync();
            var report = new List<CashierPerformanceDto>();

            var cashiers = users.Where(u => u.Role == Core.Enums.UserRole.Cajero).ToList();
            if (cashiers.Count == 0 && users.Count > 0)
            {
                cashiers = users;
            }

            if (cashiers.Count > 0)
            {
                // Attribute total period sales to the operational cashier(s) cleanly
                int count = validSales.Count;
                double totalRev = validSales.Sum(s => s.Total);
                double avgTicket = count > 0 ? totalRev / count : 0.0;
                double commission = totalRev * (defaultCommissionPct / 100.0);

                foreach (var cashier in cashiers)
                {
                    report.Add(new CashierPerformanceDto(
                        cashier.FullName,
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
            Log.Error(ex, "Error generating cashier performance report");
            return [];
        }
    }


    private static SaleDto MapToDto(SaleEntity e)
    {
        var items = JsonSerializer.Deserialize(
            e.ItemsJson,
            Ticketfy.Desktop.Core.Helpers.TicketfyJsonContext.Default.ListSaleItemSnapshotDto) ?? [];
        return new SaleDto(
            Id: e.Id,
            Date: e.Date,
            Items: items,
            Total: e.Total,
            TotalCost: e.TotalCost,
            Profit: e.Profit,
            PaidAmount: e.PaidAmount,
            ChangeAmount: e.ChangeAmount,
            PaymentMethod: e.PaymentMethod,
            CustomerId: e.CustomerId,
            IsCredit: e.IsCredit == 1,
            IsCancelled: e.IsCancelled == 1,
            CancelledAt: e.CancelledAt,
            EstadoFiscal: e.EstadoFiscal,
            UuidSat: e.UuidSat,
            SerieFolio: e.SerieFolio,
            Status: e.Status,
            InvoiceId: e.InvoiceId,
            InvoiceStatus: e.InvoiceStatus,
            CashierUserId: null,
            CashierName: null
        );
    }
}
