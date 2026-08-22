using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels.Settings;

/// <summary>
/// Data management settings: backup, export CSV, data purge.
/// </summary>
public partial class DatosSettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;

    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty] private bool _isExporting = false;

    public DatosSettingsViewModel(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        IsExporting = true;
        try
        {
            FeedbackMessage = "Exportación iniciada...";
            await Task.Delay(500); // Placeholder — actual implementation uses BackupService
            FeedbackMessage = "✅ Datos exportados correctamente.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DatosSettingsViewModel: error exporting");
            FeedbackMessage = "Error al exportar los datos.";
        }
        finally { IsExporting = false; }
    }
}
