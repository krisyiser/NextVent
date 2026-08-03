using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Core.Models;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetAllAsync();
    Task<ExpenseDto> CreateAsync(ExpenseDto dto);
    Task<bool> DeleteAsync(string id);
    Task<FinancialSummaryDto> GetFinancialSummaryAsync();
    Task<NetProfitReportModel> CalculateTrueNetProfitAsync(DateTime startDate, DateTime endDate);
}
