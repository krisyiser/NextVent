using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Core.Models;
using NextVent.Data;
using NextVent.Data.Entities;

namespace NextVent.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserModel>> GetActiveUsersAsync()
    {
        var entities = await _context.Users
            .Where(u => u.Estatus == 1)
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<UserModel?> ValidatePinAsync(string username, string pin4Digits)
    {
        var entity = await _context.Users
            .FirstOrDefaultAsync(u => u.Nombre.ToLower() == username.ToLower() || u.Id == username);

        if (entity == null || entity.Estatus != 1) return null;

        var storedPin = entity.PinChecadorHash ?? "1234";
        if (storedPin == pin4Digits)
        {
            return MapToModel(entity);
        }

        return null;
    }

    public async Task<UserModel?> ValidateAnyPinAsync(string pin4Digits)
    {
        var entities = await _context.Users
            .Where(u => u.Estatus == 1)
            .AsNoTracking()
            .ToListAsync();

        var match = entities.FirstOrDefault(u => (u.PinChecadorHash ?? "1234") == pin4Digits);
        return match != null ? MapToModel(match) : null;
    }

    public async Task<bool> ValidateAdminPinAsync(string pin4Digits)
    {
        var admins = await _context.Users
            .Where(u => u.Estatus == 1 && (u.Rol == "ADMIN" || u.Rol == "GERENTE"))
            .AsNoTracking()
            .ToListAsync();

        return admins.Any(u => (u.PinChecadorHash ?? "1234") == pin4Digits);
    }

    private static UserModel MapToModel(UserEntity entity)
    {
        var roleEnum = string.Equals(entity.Rol, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(entity.Rol, "GERENTE", StringComparison.OrdinalIgnoreCase)
            ? SystemRole.ADMIN
            : SystemRole.CAJERO;

        return new UserModel
        {
            Id = Guid.TryParse(entity.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid(),
            FullName = entity.Nombre,
            Username = entity.Nombre.ToLower().Replace(" ", ""),
            Role = roleEnum,
            Pin4Digits = entity.PinChecadorHash ?? "1234",
            IsActive = entity.Estatus == 1
        };
    }
}
