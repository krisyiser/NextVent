using System;

namespace NextVent.Core.Models;

public class NetProfitReportModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetProfitPercentage { get; set; }
}
