using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Ticketfy.Core.Messages;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Full CRUD for user accounts. Previously embedded in the monolithic SettingsViewModel.
/// Handles cashier/admin creation, 4-digit PIN management, and admin-confirmed deletion.
/// </summary>
public partial class UsuariosSettingsViewModel : ObservableObject
{
    private readonly IUserService? _userService;

    public ObservableCollection<UserDto> Users { get; } = [];

    // ── Create new user form ───────────────────────────────────────────────
    [ObservableProperty] private string _newUsername = string.Empty;
    [ObservableProperty] private string _newFullName = string.Empty;
    [ObservableProperty] private string _newRole = "CAJERO";
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _newPasswordHint = string.Empty;
    [ObservableProperty] private string _newPin1 = string.Empty;
    [ObservableProperty] private string _newPin2 = string.Empty;
    [ObservableProperty] private string _newPin3 = string.Empty;
    [ObservableProperty] private string _newPin4 = string.Empty;

    public ObservableCollection<string> RoleOptions { get; } = ["CAJERO", "SUPERVISOR", "ADMIN"];

    // ── Admin delete confirmation ──────────────────────────────────────────
    [ObservableProperty] private UserDto? _userToDelete;
    [ObservableProperty] private bool _isConfirmingAdminDelete = false;
    [ObservableProperty] private string _adminDeletePassword = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public UsuariosSettingsViewModel(IUserService? userService = null)
    {
        _userService = userService;
        if (_userService != null) _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (_userService == null) return;
        try
        {
            var list = await _userService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Users.Clear();
                foreach (var u in list) Users.Add(u);
            });
        }
        catch (Exception ex) { Log.Error(ex, "UsuariosSettingsViewModel: error loading users"); }
    }

    public Task LoadUsersAsync() => LoadAsync();

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (_userService == null) return;
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewFullName))
        {
            FeedbackMessage = "Nombre y Usuario son obligatorios";
            return;
        }

        try
        {
            string finalPass = string.IsNullOrWhiteSpace(NewPassword)
                ? string.Empty
                : Ticketfy.Core.Helpers.CryptoHelper.HashPassword(NewPassword);

            string finalPin = $"{NewPin1}{NewPin2}{NewPin3}{NewPin4}";
            if (finalPin.Length != 4)
            {
                FeedbackMessage = "El PIN debe ser de 4 dígitos";
                return;
            }

            await _userService.SaveAsync(Guid.NewGuid().ToString(), NewFullName, NewUsername, NewRole, finalPass, finalPin, NewPasswordHint);
            await LoadAsync();

            NewUsername = string.Empty;
            NewFullName = string.Empty;
            NewPin1 = string.Empty; NewPin2 = string.Empty; NewPin3 = string.Empty; NewPin4 = string.Empty;
            NewPassword = string.Empty;
            NewPasswordHint = string.Empty;
            NewRole = "CAJERO";
            FeedbackMessage = "¡Cajero / Usuario registrado correctamente!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UsuariosSettingsViewModel: error creating user");
            FeedbackMessage = "Error al crear usuario";
        }
    }

    [RelayCommand]
    private void RequestDeleteUser(UserDto user)
    {
        if (user == null) return;

        if (user.Role.ToUpper() == "ADMIN")
        {
            UserToDelete = user;
            IsConfirmingAdminDelete = true;
            AdminDeletePassword = string.Empty;
            FeedbackMessage = "Para eliminar un administrador, confirma con su contraseña.";
        }
        else
        {
            _ = ConfirmDeleteUserAsync(user);
        }
    }

    [RelayCommand]
    private void CancelAdminDelete()
    {
        IsConfirmingAdminDelete = false;
        UserToDelete = null;
        AdminDeletePassword = string.Empty;
        FeedbackMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmAdminDeleteAsync()
    {
        if (UserToDelete == null || _userService == null) return;

        string savedHash = await _userService.GetPasswordHashAsync(UserToDelete.Id) ?? string.Empty;
        bool valid = string.IsNullOrEmpty(savedHash)
            || Ticketfy.Core.Helpers.CryptoHelper.VerifyPassword(AdminDeletePassword, savedHash)
            || Ticketfy.Services.Security.SecurityManager.VerifyPassword(AdminDeletePassword, savedHash);

        if (valid)
        {
            await ConfirmDeleteUserAsync(UserToDelete);
            CancelAdminDelete();
        }
        else
        {
            FeedbackMessage = "Contraseña de administrador incorrecta. No se puede eliminar.";
        }
    }

    private async Task ConfirmDeleteUserAsync(UserDto user)
    {
        if (_userService == null || user == null) return;
        try
        {
            await _userService.DeleteAsync(user.Id);
            await LoadAsync();
            WeakReferenceMessenger.Default.Send(new UserDeletedMessage(user.Id));
            FeedbackMessage = $"Usuario {user.FullName} eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UsuariosSettingsViewModel: error deleting user");
            FeedbackMessage = "Error al eliminar usuario.";
        }
    }
}
