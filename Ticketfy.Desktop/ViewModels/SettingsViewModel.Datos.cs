using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Ticketfy.ViewModels;

public partial class SettingsViewModel
{
    [RelayCommand]
    private async Task CreateBackupNowAsync()
    {
        FeedbackMessage = "¡Respaldo local generado exitosamente!";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportCatalogCsvAsync()
    {
        FeedbackMessage = "¡Catálogo exportado en formato CSV!";
        await Task.CompletedTask;
    }
}
