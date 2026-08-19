using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Core.Models;

namespace Ticketfy.Services.Interfaces;

public interface IPerformanceAnalyticsService
{
    Task<List<CashierProductivityReportModel>> CalculateTrueCashierProductivityAsync(DateTime startDate, DateTime endDate);
}
