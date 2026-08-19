using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetAllAsync();
    Task<ExpenseDto> CreateAsync(ExpenseDto dto);
    Task<bool> DeleteAsync(string id);
    Task<FinancialSummaryDto> GetFinancialSummaryAsync();
    Task<NetProfitReportModel> CalculateTrueNetProfitAsync(DateTime startDate, DateTime endDate);
}
