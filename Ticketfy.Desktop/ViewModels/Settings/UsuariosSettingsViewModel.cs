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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ticketfy.ViewModels.Settings;

public class PermissionItemModel : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    private bool _isGranted;
    public bool IsGranted
    {
        get => _isGranted;
        set => SetProperty(ref _isGranted, value);
    }
}

/// <summary>
/// Full CRUD for user accounts and RBAC Permission Matrix management.
/// Handles cashier/admin creation, 4-digit PIN management, custom role creation, and granular permission editing per role.
/// </summary>
public partial class UsuariosSettingsViewModel : ObservableObject
{
    private readonly IUserService? _userService;
    private readonly ISettingsService? _settingsService;

    public ObservableCollection<UserDto> Users { get; } = [];

    // ── Sub-tab navigation ──────────────────────────────────────────────────
    [ObservableProperty] private bool _isUsuariosTabVisible = true;
    [ObservableProperty] private bool _isPermisosTabVisible = false;

    [RelayCommand]
    private void SelectSubTab(object? param)
    {
        int subIndex = 0;
        if (param is int iVal) subIndex = iVal;
        else if (param is string sVal && int.TryParse(sVal, out int parsed)) subIndex = parsed;

        IsUsuariosTabVisible = (subIndex == 0);
        IsPermisosTabVisible = (subIndex == 1);
    }

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

    public ObservableCollection<string> RoleOptions { get; } = ["ADMINISTRADOR", "GERENTE", "SUPERVISOR", "CAJERO", "VENDEDOR"];

    // ── Dynamic custom role creation (+ Nuevo Rol) ──────────────────────────
    [ObservableProperty] private bool _isAddingCustomRole = false;
    [ObservableProperty] private string _customRoleName = string.Empty;

    // ── Admin delete confirmation ──────────────────────────────────────────
    [ObservableProperty] private UserDto? _userToDelete;
    [ObservableProperty] private bool _isConfirmingAdminDelete = false;
    [ObservableProperty] private string _adminDeletePassword = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    // ── Granular RBAC Permissions ──────────────────────────────────────────
    [ObservableProperty] private string _selectedPermissionRole = "CAJERO";
    public ObservableCollection<PermissionItemModel> RolePermissions { get; } = [];
    [ObservableProperty] private string _permissionFeedbackMessage = string.Empty;

    [RelayCommand]
    private void ToggleAddCustomRole()
    {
        IsAddingCustomRole = !IsAddingCustomRole;
        CustomRoleName = string.Empty;
    }

    [RelayCommand]
    private void AddCustomRole()
    {
        if (string.IsNullOrWhiteSpace(CustomRoleName)) return;

        string normalized = CustomRoleName.Trim().ToUpper();
        if (!RoleOptions.Contains(normalized))
        {
            RoleOptions.Add(normalized);
        }
        NewRole = normalized;
        SelectedPermissionRole = normalized;
        CustomRoleName = string.Empty;
        IsAddingCustomRole = false;
        FeedbackMessage = $"¡Nuevo rol '{normalized}' agregado correctamente! Puede personalizar sus permisos a continuación.";
    }

    public UsuariosSettingsViewModel(IUserService? userService = null, ISettingsService? settingsService = null)
    {
        _userService = userService;
        _settingsService = settingsService;

        if (_userService != null) _ = LoadAsync();
        _ = LoadRolePermissionsAsync(SelectedPermissionRole);
    }

    partial void OnSelectedPermissionRoleChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _ = LoadRolePermissionsAsync(value);
        }
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

    // ── RBAC Permission Engine ─────────────────────────────────────────────
    public async Task LoadRolePermissionsAsync(string roleName)
    {
        var allPermissions = GetDefaultPermissionCatalog();
        Dictionary<string, bool>? savedPermissions = null;

        if (_settingsService != null)
        {
            try
            {
                var json = await _settingsService.GetAsync($"RolePermissions_{roleName.ToUpper()}");
                if (!string.IsNullOrWhiteSpace(json))
                {
                    savedPermissions = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading saved permissions for role {Role}", roleName);
            }
        }

        // Default Presets if no custom settings exist yet
        if (savedPermissions == null)
        {
            savedPermissions = GetDefaultRolePreset(roleName);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RolePermissions.Clear();
            foreach (var perm in allPermissions)
            {
                if (savedPermissions.TryGetValue(perm.Key, out bool granted))
                {
                    perm.IsGranted = granted;
                }
                RolePermissions.Add(perm);
            }
        });
    }

    [RelayCommand]
    private async Task SaveRolePermissionsAsync()
    {
        if (_settingsService == null) return;
        try
        {
            var dict = RolePermissions.ToDictionary(p => p.Key, p => p.IsGranted);
            string json = JsonSerializer.Serialize(dict);
            await _settingsService.SetAsync($"RolePermissions_{SelectedPermissionRole.ToUpper()}", json);
            PermissionFeedbackMessage = $"¡Permisos del rol '{SelectedPermissionRole}' guardados y aplicados correctamente!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving permissions for role {Role}", SelectedPermissionRole);
            PermissionFeedbackMessage = "Error al guardar los permisos del rol.";
        }
    }

    private static List<PermissionItemModel> GetDefaultPermissionCatalog() => [
        new() { Key = "pos.checkout", Category = "POS & Ventas", Title = "Procesar Cobro y Ventas", Description = "Permite realizar ventas y cobros en el terminal POS" },
        new() { Key = "pos.discount", Category = "POS & Ventas", Title = "Aplicar Descuentos Directos", Description = "Permite modificar precios o aplicar descuentos directos al carrito" },
        new() { Key = "pos.cancel", Category = "POS & Ventas", Title = "Cancelar Ventas & Devoluciones", Description = "Permite anular tickets de venta y procesar reembolsos" },

        new() { Key = "cash.open_close", Category = "Caja & Dinero", Title = "Apertura y Cierre de Caja", Description = "Permite abrir turno de caja, realizar arqueos y corte de caja" },
        new() { Key = "cash.in_out", Category = "Caja & Dinero", Title = "Entradas y Salidas de Efectivo", Description = "Permite registrar gastos directos o ingresos extra de caja" },

        new() { Key = "inventory.manage", Category = "Inventario & Productos", Title = "Gestión de Productos & Stock", Description = "Permite registrar, editar o eliminar productos en catálogo" },
        new() { Key = "inventory.pricing", Category = "Inventario & Precios", Title = "Ajustar Precios & Promociones", Description = "Permite modificar precios de venta, costos y promociones" },

        new() { Key = "customers.manage", Category = "Clientes & Crédito", Title = "Administrar Clientes", Description = "Permite alta de clientes, consulta de saldo y monedero" },
        new() { Key = "customers.credit", Category = "Clientes & Crédito", Title = "Otorgar & Ajustar Crédito", Description = "Permite modificar límites de crédito corriente de clientes" },

        new() { Key = "reports.view", Category = "Reportes & Finanzas", Title = "Ver Reportes & Ganancias", Description = "Permite consultar métricas financieras, cortes X/Z y utilidades" },
        new() { Key = "reports.export", Category = "Reportes & Finanzas", Title = "Exportar Reportes & Auditoría", Description = "Permite exportar reportes en Excel/PDF y bitácoras de auditoría" },

        new() { Key = "system.users", Category = "Administración & Sistema", Title = "Gestión de Usuarios & Roles", Description = "Permite crear, modificar o eliminar usuarios y ajustar roles RBAC" },
        new() { Key = "system.settings", Category = "Administración & Sistema", Title = "Configuración Global del Sistema", Description = "Permite modificar impresoras, datos de la empresa y temas" }
    ];

    private static Dictionary<string, bool> GetDefaultRolePreset(string role)
    {
        string norm = role.Trim().ToUpper();
        return norm switch
        {
            "ADMINISTRADOR" or "ADMIN" => new()
            {
                ["pos.checkout"] = true, ["pos.discount"] = true, ["pos.cancel"] = true,
                ["cash.open_close"] = true, ["cash.in_out"] = true,
                ["inventory.manage"] = true, ["inventory.pricing"] = true,
                ["customers.manage"] = true, ["customers.credit"] = true,
                ["reports.view"] = true, ["reports.export"] = true,
                ["system.users"] = true, ["system.settings"] = true
            },
            "GERENTE" => new()
            {
                ["pos.checkout"] = true, ["pos.discount"] = true, ["pos.cancel"] = true,
                ["cash.open_close"] = true, ["cash.in_out"] = true,
                ["inventory.manage"] = true, ["inventory.pricing"] = true,
                ["customers.manage"] = true, ["customers.credit"] = true,
                ["reports.view"] = true, ["reports.export"] = true,
                ["system.users"] = false, ["system.settings"] = false
            },
            "SUPERVISOR" => new()
            {
                ["pos.checkout"] = true, ["pos.discount"] = true, ["pos.cancel"] = true,
                ["cash.open_close"] = true, ["cash.in_out"] = true,
                ["inventory.manage"] = true, ["inventory.pricing"] = false,
                ["customers.manage"] = true, ["customers.credit"] = false,
                ["reports.view"] = true, ["reports.export"] = false,
                ["system.users"] = false, ["system.settings"] = false
            },
            "VENDEDOR" => new()
            {
                ["pos.checkout"] = true, ["pos.discount"] = false, ["pos.cancel"] = false,
                ["cash.open_close"] = false, ["cash.in_out"] = false,
                ["inventory.manage"] = false, ["inventory.pricing"] = false,
                ["customers.manage"] = true, ["customers.credit"] = false,
                ["reports.view"] = false, ["reports.export"] = false,
                ["system.users"] = false, ["system.settings"] = false
            },
            _ => new() // CAJERO and Custom Roles
            {
                ["pos.checkout"] = true, ["pos.discount"] = false, ["pos.cancel"] = false,
                ["cash.open_close"] = true, ["cash.in_out"] = true,
                ["inventory.manage"] = false, ["inventory.pricing"] = false,
                ["customers.manage"] = true, ["customers.credit"] = false,
                ["reports.view"] = false, ["reports.export"] = false,
                ["system.users"] = false, ["system.settings"] = false
            }
        };
    }

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
