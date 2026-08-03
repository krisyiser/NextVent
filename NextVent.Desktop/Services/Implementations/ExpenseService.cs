using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;

namespace NextVent.Services.Implementations;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;

    public ExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseDto>> GetAllAsync()
    {
        var entities = await _context.Expenses.AsNoTracking().OrderByDescending(e => e.Date).ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<ExpenseDto> CreateAsync(ExpenseDto dto)
    {
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
        await _context.SaveChangesAsync();
        return MapToDto(entity);
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

    private static ExpenseDto MapToDto(ExpenseEntity e) =>
        new(e.Id, e.Category, e.Amount, e.Date, e.Description, e.PaymentMethod, e.RegisteredByUser);
}
