using System.Threading.Tasks;
using Ticketfy.Core.Models;

namespace Ticketfy.Services.Interfaces;

public interface IFacturamaService
{
    Task<FacturamaCfdiResponse?> CreateInvoiceAsync(FacturamaCfdiRequest request);
}
