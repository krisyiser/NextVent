using NextVent.Data.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextVent.Services.Interfaces;

/// <summary>
/// Product CRUD operations with pagination, barcode lookup, and CSV bulk import.
/// </summary>
public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<List<ProductDto>> GetByCategoryAsync(string category);
    Task<ProductDto?> GetByBarcodeAsync(string barcode);
    Task<ProductDto?> GetByIdAsync(string id);
    Task AddAsync(ProductDto product);
    Task UpdateAsync(ProductDto product);
    Task DeleteAsync(string id);
    Task ClearInventoryAsync();
    Task BulkSaveAsync(IEnumerable<ProductDto> products);
    Task<int> ImportFromCsvTextAsync(string csvContent);
    Task<List<ProductDto>> SearchFtsAsync(string query);
    Task<IEnumerable<ProductDto>> GetCatalogForPosAsync();
    Task<bool> AdjustStockManuallyAsync(string productId, double newPhysicalStock, string reason, string userId, NextVent.Services.Security.ISecurityInterceptionService? securityService = null, IAuditService? auditService = null);
}
