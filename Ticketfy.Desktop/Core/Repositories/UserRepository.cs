using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Core.Models;
using Ticketfy.Core.Services;
using Ticketfy.Data;
using Ticketfy.Data.Entities;

namespace Ticketfy.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly IUserRolePermissionEngine _permissionEngine;

    public UserRepository(AppDbContext context, IUserRolePermissionEngine? permissionEngine = null)
    {
        _context = context;
        _permissionEngine = permissionEngine ?? new UserRolePermissionEngine();
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
        var activeUsers = await _context.Users
            .Where(u => u.IsActive)
            .AsNoTracking()
            .ToListAsync();

        return activeUsers.Any(u => (u.PinCode ?? "1234") == pin4Digits && _permissionEngine.IsAdminOrManager(u.RoleString ?? u.Role.ToString()));
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

    private UserModel MapToModel(UserEntity entity)
    {
        return _permissionEngine.MapToModel(entity);
    }
}
