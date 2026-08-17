using CommunityToolkit.Mvvm.ComponentModel;
using NextVent.Core.Helpers;
using NextVent.Core.Models;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NextVent.Services.Auth;

public record AuthenticateResult(bool IsSuccess, UserModel? User = null);

public sealed partial class AuthService : ObservableObject
{
    private readonly IUserService _userService;

    [ObservableProperty]
    private UserDto? _currentUser;

    [ObservableProperty]
    private bool _isAuthenticated;

    public AuthService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var result = await AuthenticateAsync(username, password);
        return result.IsSuccess;
    }

    public async Task<AuthenticateResult> AuthenticateAsync(string username, string password)
    {
        var user = await _userService.GetByNameAsync(username);
        if (user is null || !user.IsActive)
        {
            Log.Warning("Authentication failed: user '{Username}' not found or inactive", username);
            return new AuthenticateResult(false);
        }

        var hash = await _userService.GetPasswordHashAsync(user.Id);
        if (hash is null || (!CryptoHelper.VerifyPassword(password, hash) && !NextVent.Services.Security.SecurityManager.VerifyPassword(password, hash)))
        {
            Log.Warning("Authentication failed: invalid credentials for '{Username}'", username);
            return new AuthenticateResult(false);
        }

        var systemRole = string.Equals(user.Rol, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(user.Rol, "GERENTE", StringComparison.OrdinalIgnoreCase)
            ? SystemRole.ADMIN
            : SystemRole.CAJERO;

        var pin = await _userService.GetPinHashAsync(user.Id) ?? "1234";

        var userModel = new UserModel
        {
            Id = Guid.TryParse(user.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid(),
            FullName = user.FullName,
            Username = user.Username,
            Role = systemRole,
            Pin4Digits = pin,
            IsActive = user.IsActive
        };

        CurrentUser = user;
        IsAuthenticated = true;
        Log.Information("Login successful: {Username} ({Role})", user.Nombre, user.Rol);

        return new AuthenticateResult(true, userModel);
    }

    public async Task<string?> GetPasswordHintAsync(string username)
    {
        return await _userService.GetPasswordHintAsync(username);
    }

    public async Task<bool> VerifyManagerPinAsync(string userId, string pin)
    {
        var storedPin = await _userService.GetPinHashAsync(userId);
        return storedPin == pin;
    }

    public async Task<bool> VerifyManagerPasswordAsync(string userId, string password)
    {
        var hash = await _userService.GetPasswordHashAsync(userId);
        return hash is not null && CryptoHelper.VerifyPassword(password, hash);
    }

    public void Logout()
    {
        Log.Information("Logout: {Username}", CurrentUser?.Nombre ?? "unknown");
        CurrentUser = null;
        IsAuthenticated = false;
    }

    public bool HasRole(string requiredRole)
    {
        if (CurrentUser is null) return false;

        return requiredRole switch
        {
            "CAJERO" => true,
            "GERENTE" => CurrentUser.Rol is "GERENTE" or "ADMIN",
            "ADMIN" => CurrentUser.Rol == "ADMIN",
            _ => false
        };
    }
}
