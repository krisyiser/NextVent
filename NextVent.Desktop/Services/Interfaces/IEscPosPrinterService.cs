using System.Threading.Tasks;
using NextVent.Core.Models;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public interface IEscPosPrinterService
{
    Task<bool> PrintTicketAsync(SaleDto sale, string printerPortOrName = "COM1");
    Task<bool> OpenCashDrawerAsync(string printerPortOrName = "COM1", int drawerPin = 0);
    Task<bool> PrintNonSaleCashMovementSlipAsync(ShiftMovementSlipModel model, string printerPortOrName = "COM1");
    Task<bool> PrintTestPageAsync(string printerPortOrName = "COM1");
    Task<bool> IsPrinterOnlineAsync();
}
