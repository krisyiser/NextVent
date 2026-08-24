using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Core.Enums;
using Ticketfy.Services.Interfaces;

namespace Ticketfy.Services.Implementations;

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
        var count = await _context.Users.CountAsync();
        if (count == 0)
        {
            var adminUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                FullName = "Administrador del Sistema",
                Role = UserRole.Admin,
                PasswordHash = Ticketfy.Core.Helpers.CryptoHelper.HashPassword("admin"),
                PinCode = "1234",
                PasswordHint = "Credencial de sistema predeterminada",
                IsActive = true
            };
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
        }

        var entities = await _context.Users.AsNoTracking().ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByNameAsync(string name)
    {
        // Explicitly ONLY match the exact Username credential, never the FullName
        var entity = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == name.ToLower());
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<UserDto>> GetManagersAsync()
    {
        var entities = await _context.Users.Where(u => u.Role == UserRole.Admin || u.Role == UserRole.Gerente).AsNoTracking().ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task SaveAsync(string id, string nombre, string username, string rol, string? passwordHash, string? pinHash, string? passwordHint = null)
    {
        Guid guidId = Guid.TryParse(id, out var parsed) ? parsed : Guid.NewGuid();
        var entity = await _context.Users.FindAsync(guidId);
        
        var enumRole = Enum.TryParse<UserRole>(rol, true, out var parsedRole) ? parsedRole : UserRole.Cajero;

        if (entity == null)
        {
            entity = new UserEntity
            {
                Id = guidId,
                FullName = nombre,
                Username = username,
                Role = enumRole,
                PasswordHash = passwordHash ?? string.Empty,
                PasswordHint = passwordHint ?? string.Empty,
                PinCode = string.IsNullOrEmpty(pinHash) ? "1234" : pinHash,
                IsActive = true
            };
            _context.Users.Add(entity);
        }
        else
        {
            entity.FullName = nombre;
            entity.Role = enumRole;
            if (!string.IsNullOrEmpty(pinHash)) entity.PinCode = pinHash;
            if (!string.IsNullOrEmpty(passwordHash)) entity.PasswordHash = passwordHash;
            if (passwordHint != null) entity.PasswordHint = passwordHint;
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            var entity = await _context.Users.FindAsync(guidId);
            if (entity != null)
            {
                _context.Users.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task<string?> GetPasswordHashAsync(string userId)
    {
        if (Guid.TryParse(userId, out var guidId))
        {
            var entity = await _context.Users.FindAsync(guidId);
            return entity?.PasswordHash;
        }
        return null;
    }

    public async Task<string?> GetPinHashAsync(string userId)
    {
        if (Guid.TryParse(userId, out var guidId))
        {
            var entity = await _context.Users.FindAsync(guidId);
            return entity?.PinCode;
        }
        return null;
    }

    public async Task<string?> GetPasswordHintAsync(string username)
    {
        var entity = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        return entity?.PasswordHint;
    }

    private static UserDto MapToDto(UserEntity e)
    {
        var roleStr = e.Role.ToString().ToUpper();
        return new UserDto(e.Id.ToString(), e.Username, e.FullName, roleStr, e.IsActive);
    }
}
