using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Core.Models;

namespace NextVent.Services.Interfaces;

public interface IPerformanceAnalyticsService
{
    Task<List<CashierProductivityReportModel>> CalculateTrueCashierProductivityAsync(DateTime startDate, DateTime endDate);
}
