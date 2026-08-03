using CommunityToolkit.Mvvm.ComponentModel;
using NextVent.Core.Helpers;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using Serilog;
using System.Threading.Tasks;

namespace NextVent.Services.Auth;

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
        var user = await _userService.GetByNameAsync(username);
        if (user is null || !user.IsActive)
        {
            Log.Warning("Login failed: user '{Username}' not found or inactive", username);
            return false;
        }

        var hash = await _userService.GetPasswordHashAsync(user.Id);
        if (hash is null || !CryptoHelper.VerifyPassword(password, hash))
        {
            Log.Warning("Login failed: invalid credentials for '{Username}'", username);
            return false;
        }

        CurrentUser = user;
        IsAuthenticated = true;
        Log.Information("Login successful: {Username} ({Role})", user.Nombre, user.Rol);
        return true;
    }

    public async Task<bool> VerifyManagerPinAsync(string userId, string pin)
    {
        var hash = await _userService.GetPinHashAsync(userId);
        return hash is not null && CryptoHelper.VerifyPassword(pin, hash);
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
