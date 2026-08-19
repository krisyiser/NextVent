using System.Threading.Tasks;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

public interface IExternalCatalogService
{
    Task<ProductDto?> FetchProductByBarcodeAsync(string barcode);
}
