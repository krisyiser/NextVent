using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

/// <summary>
/// Sale transactions with atomic stock deduction, debt increment, and cancellation rollback.
/// </summary>
public interface ISaleService
{
    Task<SaleDto> SaveAsync(SaleDto sale);
    Task<List<SaleDto>> GetHistoryAsync(int limit = 500);
    Task CancelAsync(string saleId);
    Task UpdateFiscalStatusAsync(string saleId, string status, string? uuid, string? folio);
    Task<bool> ProcessPartialReturnAsync(string saleId, string productId, double returnQty, string reason, string refundMethod = "Efectivo");
    Task<List<CashierPerformanceDto>> GetCashierPerformanceReportAsync(double defaultCommissionPct = 3.0);
}
