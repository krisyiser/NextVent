using CommunityToolkit.Mvvm.ComponentModel;
using Ticketfy.Data.Dtos;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Inventory;

/// <summary>
/// Manages predictive restock alerts and intelligent inventory insights.
/// Extracted from InventoryViewModel.
/// </summary>
public partial class InventoryIntelligenceViewModel : ObservableObject
{
    private readonly IPredictiveIntelligenceService? _predictiveService;

    public ObservableCollection<PredictiveAlertDto> UrgentRestockAlerts { get; } = [];

    [ObservableProperty] private bool _isIntelligencePanelVisible = false;

    public InventoryIntelligenceViewModel(IPredictiveIntelligenceService? predictiveService = null)
    {
        _predictiveService = predictiveService;
    }

    public async Task RefreshAlertsAsync()
    {
        if (_predictiveService == null) return;
        try
        {
            var alerts = await _predictiveService.GetUrgentRestockAlertsAsync();
            UrgentRestockAlerts.Clear();
            foreach (var alert in alerts.Take(3)) UrgentRestockAlerts.Add(alert);
            IsIntelligencePanelVisible = UrgentRestockAlerts.Count > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "InventoryIntelligenceViewModel: error refreshing alerts");
        }
    }
}
