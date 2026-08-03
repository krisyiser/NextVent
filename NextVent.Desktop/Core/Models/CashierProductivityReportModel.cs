using System;

namespace NextVent.Core.Models;

public class CashierProductivityReportModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public double TotalHoursWorked { get; set; }
    public int TotalTicketsProcessed { get; set; }
    public double GrossSales { get; set; }
    public double AverageTicketValue { get; set; }
    public double TrueSalesPerHour { get; set; }
    public double TicketsPerHour { get; set; }
    public double EstimatedCommission { get; set; }
}
