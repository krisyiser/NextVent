using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Core.Models.Settings;
using Ticketfy.Services.Interfaces;
using Ticketfy.Services.Settings;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Unified Reactive Settings ViewModel orchestrating the 4 Key Operational Axes under Protocol Valcore v4.0.
/// Replaces fragmented sub-ViewModels with atomic AppSettings POCO state, instant ThemeEngine reactive dispatch,
/// and 300ms debounced persistence to SQLite storage.
/// </summary>
public partial class UnifiedSettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;
    private readonly ThemeEngine _themeEngine = ThemeEngine.Instance;
    private CancellationTokenSource? _saveDebounceCts;

    // 4 Key Operational Axes State
    [ObservableProperty] private bool _isUiTab = true;
    [ObservableProperty] private bool _isCompanyTab = false;
    [ObservableProperty] private bool _isHardwareTab = false;
    [ObservableProperty] private bool _isSystemTab = false;

    // Atomic Master AppSettings State POCO
    [ObservableProperty] private AppSettings _state = new();

    // Feedback Message
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    // Color Swatch Palette Options
    public ObservableCollection<ColorPaletteItem> AccentColors { get; } = [
        new("Azul Zafiro", "#3B82F6"),
        new("Verde Esmeralda", "#10B981"),
        new("Índigo Profundo", "#6366F1"),
        new("Carmesí Rubí", "#E11D48"),
        new("Ámbar Dorado", "#F59E0B"),
        new("Cian Eléctrico", "#06B6D4"),
        new("Púrpura Neón", "#8B5CF6"),
        new("Gris Acero", "#64748B")
    ];

    public ObservableCollection<string> ThemePresetNames { get; } = [
        "Modo Oscuro", "Modo Claro", "Nordic Slate", "Cyberpunk Dark",
        "Emerald Glass", "Alto Contraste", "Retro Amber"
    ];

    public ObservableCollection<string> AvailableFontOptions { get; } = [
        "Inter", "Roboto", "Montserrat", "Poppins", "JetBrains Mono", "Fira Code",
        "Outfit", "Plus Jakarta Sans", "SF Pro Display", "Open Sans", "Lato", "Consolas"
    ];

    public ObservableCollection<string> PaperWidthOptions { get; } = [
        "80mm (Estándar POS)", "58mm (Térmica Mini)", "Carta / A4 (PDF Fiscal)"
    ];

    public UnifiedSettingsViewModel(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
        if (_settingsService != null) _ = LoadAsync();
    }

    [RelayCommand]
    private void SelectAxisTab(string axis)
    {
        IsUiTab        = axis == "ui";
        IsCompanyTab   = axis == "company";
        IsHardwareTab  = axis == "hardware";
        IsSystemTab    = axis == "system";
    }

    [RelayCommand]
    private void SelectTheme(string themeName)
    {
        State.Visual.ThemeName = themeName;
        OnVisualStateChanged();
    }

    [RelayCommand]
    private void SelectPrimaryColor(string hexColor)
    {
        State.Visual.PrimaryColor = hexColor;
        OnVisualStateChanged();
    }

    [RelayCommand]
    private void SelectDensity(UIDensity density)
    {
        State.Visual.Density = density;
        OnVisualStateChanged();
    }

    /// <summary>
    /// Invoked whenever any visual customization parameter is modified by the user.
    /// Dispatches instant style injection to Avalonia resources and schedules debounced save.
    /// </summary>
    public void OnVisualStateChanged()
    {
        _themeEngine.Apply(State);
        ScheduleDebouncedSave();
    }

    public async Task LoadAsync()
    {
        if (_settingsService == null) return;
        try
        {
            State = await _settingsService.GetAppSettingsAsync();
            _themeEngine.Apply(State);
            Log.Information("UnifiedSettingsViewModel loaded AppSettings successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed loading AppSettings in UnifiedSettingsViewModel");
        }
    }

    public async Task SaveAsync()
    {
        if (_settingsService == null) return;
        try
        {
            await _settingsService.SaveAppSettingsAsync(State);
            _themeEngine.Apply(State);
            FeedbackMessage = "¡Configuración guardada correctamente!";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed saving AppSettings in UnifiedSettingsViewModel");
            FeedbackMessage = "Error guardando la configuración.";
        }
    }

    private void ScheduleDebouncedSave()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts = new CancellationTokenSource();
        var token = _saveDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested && _settingsService != null)
                {
                    await _settingsService.SaveAppSettingsAsync(State);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing debounced settings save");
            }
        }, token);
    }
}
