using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Security settings: idle lock timeout, PIN policy, session rules.
/// </summary>
public partial class SeguridadSettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;

    public ObservableCollection<int> IdleTimeoutOptions { get; } = [1, 2, 5, 10, 15, 30];
    [ObservableProperty] private int _idleTimeoutMinutes = 5;
    [ObservableProperty] private bool _requirePinOnReopen = true;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public SeguridadSettingsViewModel(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
        if (_settingsService != null) _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (_settingsService == null) return;
        try
        {
            var d = await _settingsService.GetAllAsync();
            if (d.TryGetValue("IdleTimeoutMinutes", out var ito) && int.TryParse(ito, out var itv)) IdleTimeoutMinutes = itv;
        }
        catch (Exception ex) { Log.Error(ex, "SeguridadSettingsViewModel: error loading"); }
    }

    public async Task SaveAsync()
    {
        if (_settingsService == null) return;
        await _settingsService.SetAsync("IdleTimeoutMinutes", IdleTimeoutMinutes.ToString());
        FeedbackMessage = "¡Configuración de seguridad guardada!";
    }

    [RelayCommand] private async Task Save() => await SaveAsync();
}
