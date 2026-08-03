using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;

namespace NextVent.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var entities = await _context.Users.AsNoTracking().ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByNameAsync(string name)
    {
        var entity = await _context.Users.FirstOrDefaultAsync(u => u.Nombre.ToLower() == name.ToLower());
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<UserDto>> GetManagersAsync()
    {
        var entities = await _context.Users.Where(u => u.Rol == "ADMIN").AsNoTracking().ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task SaveAsync(string id, string nombre, string rol, string? passwordHash, string? pinHash)
    {
        var entity = await _context.Users.FindAsync(id);
        if (entity == null)
        {
            entity = new UserEntity
            {
                Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
                Nombre = nombre,
                Rol = rol,
                PasswordHash = passwordHash,
                PinChecadorHash = string.IsNullOrEmpty(pinHash) ? "1234" : pinHash,
                Estatus = 1
            };
            _context.Users.Add(entity);
        }
        else
        {
            entity.Nombre = nombre;
            entity.Rol = rol;
            if (!string.IsNullOrEmpty(pinHash)) entity.PinChecadorHash = pinHash;
            if (!string.IsNullOrEmpty(passwordHash)) entity.PasswordHash = passwordHash;
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Users.FindAsync(id);
        if (entity != null)
        {
            _context.Users.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string?> GetPasswordHashAsync(string userId)
    {
        var entity = await _context.Users.FindAsync(userId);
        return entity?.PasswordHash;
    }

    public async Task<string?> GetPinHashAsync(string userId)
    {
        var entity = await _context.Users.FindAsync(userId);
        return entity?.PinChecadorHash;
    }

    private static UserDto MapToDto(UserEntity e) =>
        new(e.Id, e.Nombre.ToLower().Replace(" ", ""), e.Nombre, e.Rol, e.Estatus == 1);
}
