using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Alert configuration: low stock thresholds, email notifications.
/// </summary>
public partial class AlertasSettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;

    [ObservableProperty] private int _lowStockAlertThreshold = 5;
    [ObservableProperty] private bool _sendEmailAlerts = false;
    [ObservableProperty] private string _alertEmailRecipient = string.Empty;
    [ObservableProperty] private string _feedbackMessage = string.Empty;

    public AlertasSettingsViewModel(ISettingsService? settingsService = null)
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
            if (d.TryGetValue("LowStockAlertThreshold", out var lst) && int.TryParse(lst, out var lstv)) LowStockAlertThreshold = lstv;
            if (d.TryGetValue("SendEmailAlerts", out var sea)) SendEmailAlerts = sea?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (d.TryGetValue("AlertEmailRecipient", out var aer)) AlertEmailRecipient = aer;
        }
        catch (Exception ex) { Log.Error(ex, "AlertasSettingsViewModel: error loading"); }
    }

    public async Task SaveAsync()
    {
        if (_settingsService == null) return;
        await _settingsService.SetAsync("LowStockAlertThreshold", LowStockAlertThreshold.ToString());
        await _settingsService.SetAsync("SendEmailAlerts", SendEmailAlerts.ToString().ToLower());
        await _settingsService.SetAsync("AlertEmailRecipient", AlertEmailRecipient);
        FeedbackMessage = "¡Configuración de alertas guardada!";
    }

    [RelayCommand] private async Task Save() => await SaveAsync();
}
