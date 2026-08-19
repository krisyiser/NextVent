using System.Threading.Tasks;
using NextVent.Core.Models;

namespace NextVent.Services.Interfaces;

public interface IFacturamaService
{
    Task<FacturamaCfdiResponse?> CreateInvoiceAsync(FacturamaCfdiRequest request);
}
