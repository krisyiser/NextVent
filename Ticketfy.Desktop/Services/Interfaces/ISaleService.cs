using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

/// <summary>
/// Sale transactions with atomic stock deduction, debt increment, and cancellation rollback.
/// </summary>
public interface ISaleService
{
    event System.Action<SaleDto>? SaleSaved;
    Task<SaleDto> SaveAsync(SaleDto sale);
    Task<SaleResultModel> ProcessSaleAsync(SaleCreationDto dto);
    Task<SaleDto?> GetByIdAsync(string saleId);
    Task<List<SaleDto>> GetHistoryAsync(int limit = 500);
    Task CancelAsync(string saleId);
    Task<bool> CancelSaleAsync(string saleId, string reason);
    Task<List<SaleDto>> GetSalesByDateRangeAsync(System.DateTime start, System.DateTime end);
    Task UpdateFiscalStatusAsync(string saleId, string status, string? uuid, string? folio);
    Task<bool> ProcessPartialReturnAsync(string saleId, string productId, double returnQty, string reason, string refundMethod = "Efectivo", bool isProductInGoodCondition = true);
    /// <summary>
    /// Computes aggregated cashier sales metrics and earned commissions within an optional date window, preventing un-bounded database memory dumps.
    /// </summary>
    Task<List<CashierPerformanceDto>> GetCashierPerformanceReportAsync(System.DateTime? startDate = null, System.DateTime? endDate = null, double defaultCommissionPct = 0.0);
}

