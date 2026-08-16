using System.Threading.Tasks;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public interface IPrintDispatcherService
{
    Task DispatchSaleDocumentsAsync(SaleDto sale);
}
