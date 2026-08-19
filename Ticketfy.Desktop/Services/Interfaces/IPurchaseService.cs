using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

public interface IPurchaseService
{
    Task<List<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto?> GetByIdAsync(string id);
    Task<PurchaseDto> RegisterPurchaseAsync(PurchaseDto dto);
}
