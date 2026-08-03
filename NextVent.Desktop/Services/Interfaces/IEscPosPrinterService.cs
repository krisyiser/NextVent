using System.Threading.Tasks;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public interface IEscPosPrinterService
{
    Task<bool> PrintTicketAsync(SaleDto sale, string printerPortOrName);
    Task<bool> OpenCashDrawerAsync(string printerPortOrName);
    Task<bool> PrintTestPageAsync(string printerPortOrName);
}
