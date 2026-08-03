using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Core.Models;
using NextVent.Data;
using NextVent.Services.Interfaces;
using Serilog;

namespace NextVent.Services.Implementations;

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly AppDbContext _dbContext;

    public PerformanceAnalyticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CashierProductivityReportModel>> CalculateTrueCashierProductivityAsync(DateTime startDate, DateTime endDate)
    {
        var resultList = new List<CashierProductivityReportModel>();

        try
        {
            var activeUsers = await _dbContext.Users.AsNoTracking().ToListAsync();

            foreach (var user in activeUsers)
            {
                // 1. COMPUTE TOTAL REAL WORKED HOURS FROM ATTENDANCE LEDGER
                var attendances = await _dbContext.Attendances.AsNoTracking()
                    .Where(a => a.UserId == user.Id && a.CheckInTime >= startDate && a.CheckInTime <= endDate)
                    .ToListAsync();

                double totalHoursWorked = attendances.Sum(a => a.TotalWorkedHours);

                // 2. COMPUTE GROSS SALES & TRANSACTION COUNT
                var startIso = startDate.ToString("s");
                var endIso = endDate.ToString("s");

                var userSales = await _dbContext.Sales.AsNoTracking()
                    .Where(s => string.Compare(s.Date, startIso) >= 0 && string.Compare(s.Date, endIso) <= 0)
                    .ToListAsync();

                double totalSalesAmount = userSales.Sum(s => s.Total);
                int totalTickets = userSales.Count;

                // 3. APPLY TRUE TIME-WEIGHTED METRICS
                double salesPerWorkedHour = totalHoursWorked > 0
                    ? Math.Round(totalSalesAmount / totalHoursWorked, 2)
                    : (totalSalesAmount > 0 ? totalSalesAmount : 0.0);

                double ticketsPerWorkedHour = totalHoursWorked > 0
                    ? Math.Round((double)totalTickets / totalHoursWorked, 2)
                    : 0.0;

                double averageTicketValue = totalTickets > 0
                    ? Math.Round(totalSalesAmount / totalTickets, 2)
                    : 0.0;

                resultList.Add(new CashierProductivityReportModel
                {
                    UserId = user.Id,
                    FullName = user.Nombre,
                    Role = user.Rol,
                    TotalHoursWorked = Math.Round(totalHoursWorked, 2),
                    TotalTicketsProcessed = totalTickets,
                    GrossSales = totalSalesAmount,
                    AverageTicketValue = averageTicketValue,
                    TrueSalesPerHour = salesPerWorkedHour,
                    TicketsPerHour = ticketsPerWorkedHour,
                    EstimatedCommission = Math.Round(totalSalesAmount * 0.015, 2) // 1.5% commission
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating true cashier productivity");
        }

        return resultList.OrderByDescending(r => r.TrueSalesPerHour).ToList();
    }
}
