using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Core.Models;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

/// <summary>
/// Sale transactions with atomic stock deduction, debt increment, and cancellation rollback.
/// </summary>
public interface ISaleService
{
    Task<SaleDto> SaveAsync(SaleDto sale);
    Task<SaleResultModel> ProcessSaleAsync(SaleCreationDto dto);
    Task<SaleDto?> GetByIdAsync(string saleId);
    Task<List<SaleDto>> GetHistoryAsync(int limit = 500);
    Task CancelAsync(string saleId);
    Task<bool> CancelSaleAsync(string saleId, string reason);
    Task<List<SaleDto>> GetSalesByDateRangeAsync(System.DateTime start, System.DateTime end);
    Task UpdateFiscalStatusAsync(string saleId, string status, string? uuid, string? folio);
    Task<bool> ProcessPartialReturnAsync(string saleId, string productId, double returnQty, string reason, string refundMethod = "Efectivo", bool isProductInGoodCondition = true);
    Task<List<CashierPerformanceDto>> GetCashierPerformanceReportAsync(double defaultCommissionPct = 3.0);
}
