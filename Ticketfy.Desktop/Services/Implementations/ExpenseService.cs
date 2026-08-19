using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;

namespace Ticketfy.Services.Implementations;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;
    private readonly IEscPosPrinterService? _printerService;

    public ExpenseService(AppDbContext context, IEscPosPrinterService? printerService = null)
    {
        _context = context;
        _printerService = printerService;
    }

    public async Task<List<ExpenseDto>> GetAllAsync()
    {
        var entities = await _context.Expenses.AsNoTracking().OrderByDescending(e => e.Date).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<ExpenseDto> CreateAsync(ExpenseDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsOpen == 1);

            var entity = new ExpenseEntity
            {
                Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
                Category = string.IsNullOrEmpty(dto.Category) ? "General" : dto.Category,
                Amount = dto.Amount,
                Date = string.IsNullOrEmpty(dto.Date) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : dto.Date,
                Description = dto.Description,
                PaymentMethod = dto.PaymentMethod,
                RegisteredByUser = string.IsNullOrEmpty(dto.RegisteredByUser) ? "admin" : dto.RegisteredByUser
            };

            _context.Expenses.Add(entity);

            // Inject Cash Outflow Movement to Active Shift Drawer if Paid in Cash
            bool isCash = dto.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) || dto.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);

            if (activeShift != null && isCash)
            {
                var outflow = new ShiftMovementEntity
                {
                    ShiftId = activeShift.Id,
                    MovementType = Ticketfy.Core.Enums.MovementType.GastoOperativo,
                    Amount = dto.Amount,
                    IsOutflow = true,
                    Description = $"Gasto: {entity.Category} - {entity.Description}",
                    ReferenceId = entity.Id,
                    Timestamp = DateTime.Now.ToString("s")
                };
                _context.ShiftMovements.Add(outflow);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Trigger Cash Drawer Kick & Thermal Audit Slip if Paid in Cash
            if (isCash && _printerService != null)
            {
                _ = _printerService.OpenCashDrawerAsync("COM1");
                _ = _printerService.PrintNonSaleCashMovementSlipAsync(new Ticketfy.Core.Models.ShiftMovementSlipModel
                {
                    Folio = entity.Id.Substring(0, Math.Min(8, entity.Id.Length)).ToUpper(),
                    MovementTypeLabel = $"GASTO OPERATIVO - {entity.Category.ToUpper()}",
                    Amount = dto.Amount,
                    Description = dto.Description,
                    CashierName = dto.RegisteredByUser ?? "CAJERO EN TURNO",
                    Timestamp = DateTime.Now
                });
            }

            return MapToDto(entity);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var entity = await _context.Expenses.FindAsync(id);
        if (entity != null)
        {
            _context.Expenses.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync()
    {
        var sales = await _context.Sales.Where(s => s.IsCancelled == 0).ToListAsync();
        var expenses = await _context.Expenses.ToListAsync();

        double totalRevenue = sales.Sum(s => s.Total);
        double totalCostOfGoodsSold = sales.Sum(s => s.TotalCost);
        double grossProfit = totalRevenue - totalCostOfGoodsSold;
        double totalExpenses = expenses.Sum(e => e.Amount);
        double netProfit = grossProfit - totalExpenses;

        return new FinancialSummaryDto(totalRevenue, totalCostOfGoodsSold, grossProfit, totalExpenses, netProfit);
    }

    public async Task<Ticketfy.Core.Models.NetProfitReportModel> CalculateTrueNetProfitAsync(DateTime startDate, DateTime endDate)
    {
        var validSales = _context.Sales
            .AsNoTracking()
            .Where(s => s.IsCancelled == 0);

        decimal grossSales = (decimal)(await validSales.SumAsync(s => (double?)s.Total) ?? 0.0);
        decimal totalCogs = (decimal)(await validSales.SumAsync(s => (double?)s.TotalCost) ?? 0.0);

        var validReturns = _context.Returns.AsNoTracking();
        decimal totalRefunds = (decimal)(await validReturns.SumAsync(r => (double?)r.TotalRefunded) ?? 0.0);
        decimal cogsReversed = (decimal)(await validReturns.SumAsync(r => (double?)r.CogsReversed) ?? 0.0);

        decimal operatingExpenses = (decimal)(await _context.Expenses.SumAsync(e => (double?)e.Amount) ?? 0.0);

        decimal netSales = grossSales - totalRefunds;
        decimal effectiveCogs = totalCogs - cogsReversed;
        decimal grossProfit = netSales - effectiveCogs;
        decimal netProfit = grossProfit - operatingExpenses;

        return new Ticketfy.Core.Models.NetProfitReportModel
        {
            StartDate = startDate,
            EndDate = endDate,
            GrossSales = grossSales,
            TotalRefunds = totalRefunds,
            NetSales = netSales,
            CostOfGoodsSold = effectiveCogs,
            GrossProfit = grossProfit,
            GrossMarginPercentage = netSales > 0 ? Math.Round((grossProfit / netSales) * 100m, 2) : 0m,
            OperatingExpenses = operatingExpenses,
            NetProfit = netProfit,
            NetProfitPercentage = netSales > 0 ? Math.Round((netProfit / netSales) * 100m, 2) : 0m
        };
    }

    private static ExpenseDto MapToDto(ExpenseEntity e) =>
        new(e.Id, e.Category, e.Amount, e.Date, e.Description, e.PaymentMethod, e.RegisteredByUser);
}
