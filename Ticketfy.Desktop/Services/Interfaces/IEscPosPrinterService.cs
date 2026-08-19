using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

public interface IEscPosPrinterService
{
    Task<bool> PrintTicketAsync(SaleDto sale, string printerPortOrName = "COM1");
    Task<bool> OpenCashDrawerAsync(string printerPortOrName = "COM1", int drawerPin = 0);
    Task<bool> PrintNonSaleCashMovementSlipAsync(ShiftMovementSlipModel model, string printerPortOrName = "COM1");
    Task<bool> PrintTestPageAsync(string printerPortOrName = "COM1");
    Task<bool> IsPrinterOnlineAsync();
    Task<bool> PrintInventoryChecklistAsync(System.Collections.Generic.List<ProductDto> products, string printerPortOrName = "COM1");
    Task<bool> PrintSnapshotChecklistAsync(Ticketfy.Data.Entities.InventorySnapshotEntity snapshot, string printerPortOrName = "COM1");
    Task<bool> PrintPurchaseOrderAsync(Ticketfy.Data.Dtos.PurchaseDto purchase, string printerPortOrName = "COM1");
}
