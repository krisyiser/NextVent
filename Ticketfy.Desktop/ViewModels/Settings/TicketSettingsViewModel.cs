using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Manages all ticket and thermal printer configuration.
/// </summary>
public partial class TicketSettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;

    public ObservableCollection<string> PaperWidths { get; } = ["58mm (32 Columnas)", "80mm (48 Columnas)"];
    [ObservableProperty] private string _selectedPaperWidth = "80mm (48 Columnas)";
    [ObservableProperty] private bool _autoPrintTicketOnCheckout = true;
    [ObservableProperty] private bool _autoCutPaper = true;
    [ObservableProperty] private bool _printCashierName = true;
    [ObservableProperty] private string _ticketHeaderLine1 = string.Empty;
    [ObservableProperty] private string _ticketHeaderLine2 = string.Empty;
    [ObservableProperty] private string _ticketFooterLine1 = string.Empty;
    [ObservableProperty] private string _ticketFooterLine2 = string.Empty;

    public TicketSettingsViewModel(ISettingsService? settingsService = null)
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
            if (d.TryGetValue("PaperWidth", out var pw)) SelectedPaperWidth = pw;
            if (d.TryGetValue("Header1", out var h1)) TicketHeaderLine1 = h1;
            if (d.TryGetValue("Footer1", out var f1)) TicketFooterLine1 = f1;
        }
        catch (Exception ex) { Log.Error(ex, "TicketSettingsViewModel: error loading"); }
    }

    public async Task SaveAsync()
    {
        if (_settingsService == null) return;
        await _settingsService.SetAsync("PaperWidth", SelectedPaperWidth);
        await _settingsService.SetAsync("Header1", TicketHeaderLine1);
        await _settingsService.SetAsync("Footer1", TicketFooterLine1);
    }

    [RelayCommand] private async Task Save() => await SaveAsync();
}
