using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Core.Models;
using NextVent.Core.Enums;
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
            .Where(u => u.IsActive)
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<UserModel?> ValidatePinAsync(string username, string pin4Digits)
    {
        var entity = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() || u.FullName.ToLower() == username.ToLower() || u.Id.ToString() == username);

        if (entity == null || !entity.IsActive) return null;

        var storedPin = entity.PinCode ?? "1234";
        if (storedPin == pin4Digits)
        {
            return MapToModel(entity);
        }

        return null;
    }

    public async Task<UserModel?> ValidateAnyPinAsync(string pin4Digits)
    {
        var entities = await _context.Users
            .Where(u => u.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var match = entities.FirstOrDefault(u => (u.PinCode ?? "1234") == pin4Digits);
        return match != null ? MapToModel(match) : null;
    }

    public async Task<bool> ValidateAdminPinAsync(string pin4Digits)
    {
        var admins = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.Gerente))
            .AsNoTracking()
            .ToListAsync();

        return admins.Any(u => (u.PinCode ?? "1234") == pin4Digits);
    }

    public async Task<bool> HasAnyUsersAsync()
    {
        return await _context.Users.AnyAsync();
    }

    public async Task CreateUserAsync(UserEntity user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    private static UserModel MapToModel(UserEntity entity)
    {
        var roleEnum = entity.Role == UserRole.Admin || entity.Role == UserRole.Gerente
            ? SystemRole.ADMIN
            : SystemRole.CAJERO;

        return new UserModel
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Username = entity.Username,
            Role = roleEnum,
            Pin4Digits = entity.PinCode,
            IsActive = entity.IsActive
        };
    }
}
