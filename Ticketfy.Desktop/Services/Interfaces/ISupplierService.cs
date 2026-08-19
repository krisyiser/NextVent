using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(string id);
    Task<SupplierDto> CreateAsync(SupplierDto dto);
    Task<SupplierDto> UpdateAsync(SupplierDto dto);
    Task<bool> DeleteAsync(string id);
}
