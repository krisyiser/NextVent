using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services;
using Ticketfy.Services.Interfaces;
using Ticketfy.ViewModels.Settings;
using System;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

public record ColorPaletteItem(string Name, string HexColor);
public record FontSizeScaleOption(string Name, string SizePx, string Description);
public record KeyboardShortcutItem(string Shortcut, string ActionName, string Category);

/// <summary>
/// Main settings tab coordinator.
/// Consolidated under Protocol Valcore v4.0 with UnifiedSettingsViewModel and 4 Key Operational Axes.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService = ThemeService.Instance;
    private readonly IUserService? _userService;
    private readonly ISettingsService? _settingsService;

    // Application Version Information
    public string AppVersion => Ticketfy.Core.Helpers.AppVersionHelper.DisplayVersion;
    public string CurrentAppVersion => Ticketfy.Core.Helpers.AppVersionHelper.DisplayVersion;
    public string FullAppVersionTitle => Ticketfy.Core.Helpers.AppVersionHelper.FullTitle;

    // Master Unified ViewModel (4-Axis Engine)
    public UnifiedSettingsViewModel UnifiedVM { get; }

    // Sub-ViewModels (Legacy compatibility fallbacks)
    public EmpresaSettingsViewModel EmpresaVM { get; }
    public InterfazSettingsViewModel InterfazVM { get; }
    public TicketSettingsViewModel TicketVM { get; }
    public ConexionesSettingsViewModel ConexionesVM { get; }
    public UsuariosSettingsViewModel UsuariosVM { get; }
    public SeguridadSettingsViewModel SeguridadVM { get; }
    public AlertasSettingsViewModel AlertasVM { get; }
    public DatosSettingsViewModel DatosVM { get; }
    public AtajosSettingsViewModel AtajosVM { get; }
    public AcercaDeSettingsViewModel AcercaDeVM { get; }

    // Active Main Tab State
    [ObservableProperty] private bool _isEmpresaTab = false;
    [ObservableProperty] private bool _isInterfazTab = true; // Default to Live Customizer UI
    [ObservableProperty] private bool _isTicketTab = false;
    [ObservableProperty] private bool _isConexionesTab = false;
    [ObservableProperty] private bool _isSeguridadTab = false;
    [ObservableProperty] private bool _isDatosTab = false;
    [ObservableProperty] private bool _isAlertasTab = false;
    [ObservableProperty] private bool _isUsuariosTab = false;
    [ObservableProperty] private bool _isAtajosTab = false;
    [ObservableProperty] private bool _isAcercaDeTab = false;

    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public event Func<Task>? SettingsSaved;

    public SettingsViewModel(IUserService userService, ISettingsService settingsService)
    {
        _userService = userService;
        _settingsService = settingsService;

        UnifiedVM = new UnifiedSettingsViewModel(settingsService);
        EmpresaVM = new EmpresaSettingsViewModel(settingsService);
        InterfazVM = new InterfazSettingsViewModel(settingsService);
        TicketVM = new TicketSettingsViewModel(settingsService);
        ConexionesVM = new ConexionesSettingsViewModel(settingsService);
        UsuariosVM = new UsuariosSettingsViewModel(userService);
        SeguridadVM = new SeguridadSettingsViewModel(settingsService);
        AlertasVM = new AlertasSettingsViewModel(settingsService);
        DatosVM = new DatosSettingsViewModel(settingsService);
        AtajosVM = new AtajosSettingsViewModel();
        AcercaDeVM = new AcercaDeSettingsViewModel();

        if (_userService != null) _ = LoadUsersAsync();
    }

    public SettingsViewModel() : this(null!, null!) { }

    [RelayCommand]
    private void SelectMainTab(string tab)
    {
        IsInterfazTab   = tab == "interfaz" || tab == "ui";
        IsEmpresaTab    = tab == "empresa" || tab == "company";
        IsTicketTab     = tab == "ticket" || tab == "hardware";
        IsConexionesTab = tab == "conexiones";
        IsSeguridadTab  = tab == "seguridad" || tab == "system";
        IsDatosTab      = tab == "datos";
        IsAlertasTab    = tab == "alertas";
        IsUsuariosTab   = tab == "usuarios";
        IsAtajosTab     = tab == "atajos";
        IsAcercaDeTab   = tab == "acercade";
    }

    [RelayCommand] private void Close() { }

    [RelayCommand]
    private async Task SaveAllSettingsAsync()
    {
        await UnifiedVM.SaveAsync();
        await EmpresaVM.SaveAsync();
        await InterfazVM.SaveAsync();
        await TicketVM.SaveAsync();
        await ConexionesVM.SaveAsync();
        await SeguridadVM.SaveAsync();

        FeedbackMessage = "¡Todos los ajustes fueron guardados correctamente!";
        if (SettingsSaved != null) await SettingsSaved.Invoke();
    }
}
