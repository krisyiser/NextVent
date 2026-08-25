using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;

namespace Ticketfy.Services.Implementations;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _context;

    public SupplierService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var entities = await _context.Suppliers.AsNoTracking().ToListAsync();
        if (entities.Count == 0 || !entities.Any(e => e.Name.ToUpper().Contains("GENERAL")))
        {
            var defaultGeneral = new SupplierEntity
            {
                Id = "sup_compra_general",
                Name = "COMPRA GENERAL",
                Rfc = "XAXX010101000",
                Phone = "0000000000",
                Email = "compras@ticketfy.mx",
                Address = "Mostrador Principal / Compras Directas",
                ContactPerson = "Administrador",
                IsActive = 1
            };
            if (!entities.Any(e => e.Id == defaultGeneral.Id))
            {
                _context.Suppliers.Add(defaultGeneral);
                await _context.SaveChangesAsync();
                entities = await _context.Suppliers.AsNoTracking().ToListAsync();
            }
        }
        return entities.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(string id)
    {
        var entity = await _context.Suppliers.FindAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<SupplierDto> CreateAsync(SupplierDto dto)
    {
        var entity = new SupplierEntity
        {
            Id = string.IsNullOrEmpty(dto.Id) ? System.Guid.NewGuid().ToString() : dto.Id,
            Name = dto.Name,
            Rfc = dto.Rfc,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            ContactPerson = dto.ContactPerson,
            IsActive = dto.IsActive ? 1 : 0
        };

        _context.Suppliers.Add(entity);
        await _context.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<SupplierDto> UpdateAsync(SupplierDto dto)
    {
        var entity = await _context.Suppliers.FindAsync(dto.Id);
        if (entity != null)
        {
            entity.Name = dto.Name;
            entity.Rfc = dto.Rfc;
            entity.Phone = dto.Phone;
            entity.Email = dto.Email;
            entity.Address = dto.Address;
            entity.ContactPerson = dto.ContactPerson;
            entity.IsActive = dto.IsActive ? 1 : 0;
            await _context.SaveChangesAsync();
        }
        return dto;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var entity = await _context.Suppliers.FindAsync(id);
        if (entity != null)
        {
            _context.Suppliers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    private static SupplierDto MapToDto(SupplierEntity e) =>
        new(e.Id, e.Name, e.Rfc, e.Phone, e.Email, e.Address, e.ContactPerson, e.IsActive == 1);
}
