using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

public interface IPurchaseService
{
    Task<List<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto?> GetByIdAsync(string id);
    Task<PurchaseDto> RegisterPurchaseAsync(PurchaseDto dto);
}
