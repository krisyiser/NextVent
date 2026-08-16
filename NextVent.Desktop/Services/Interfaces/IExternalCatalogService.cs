using System.Threading.Tasks;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public interface IExternalCatalogService
{
    Task<ProductDto?> FetchProductByBarcodeAsync(string barcode);
}
