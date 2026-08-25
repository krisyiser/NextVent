using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Implementations;

namespace Ticketfy.ViewModels;

public partial class InventorySnapshotsViewModel : ObservableObject
{
    private readonly InventorySnapshotService _snapshotService;

    [ObservableProperty]
    private ObservableCollection<InventorySnapshotEntity> _snapshots = new();

    [ObservableProperty]
    private InventorySnapshotEntity? _selectedSnapshot;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    partial void OnSelectedSnapshotChanged(InventorySnapshotEntity? value)
    {
        if (value != null && (value.Items == null || value.Items.Count == 0))
        {
            _ = LoadSnapshotDetailsAsync(value.Id);
        }
    }

    private async Task LoadSnapshotDetailsAsync(string id)
    {
        IsLoading = true;
        try
        {
            var details = await _snapshotService.GetSnapshotDetailsAsync(id);
            if (details != null && SelectedSnapshot?.Id == id)
            {
                SelectedSnapshot = details;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public InventorySnapshotsViewModel()
    {
        _snapshotService = new InventorySnapshotService();
        _ = LoadSnapshotsAsync();
    }

    private async Task LoadSnapshotsAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _snapshotService.GetSnapshotsAsync();
            Snapshots = new ObservableCollection<InventorySnapshotEntity>(list);
            if (Snapshots.Count > 0 && SelectedSnapshot == null)
            {
                SelectedSnapshot = Snapshots[0];
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task PrintSnapshotAsync()
    {
        if (SelectedSnapshot == null)
        {
            if (Snapshots.Count > 0)
                SelectedSnapshot = Snapshots[0];
            else
                return;
        }
        
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var ownerWin = System.Linq.Enumerable.FirstOrDefault(desktop.Windows, w => w.IsActive && w.IsVisible) ?? desktop.MainWindow;

        var vm = new Ticketfy.ViewModels.Dialogs.PrintPreviewWindowViewModel($"Reporte de Captura Física: {SelectedSnapshot.CreatedAt:g}");
        var win = new Ticketfy.Views.Dialogs.PrintPreviewWindow { DataContext = vm };
        
        var confirmed = await win.ShowDialog<bool>(ownerWin);
        if (!confirmed) return;

        var printerSvc = new Ticketfy.Services.Implementations.EscPosPrinterService();
        bool printed = await printerSvc.PrintSnapshotChecklistAsync(SelectedSnapshot);
        if (printed)
        {
            StatusMessage = "¡Reporte de captura física enviado a la impresora térmica con éxito!";
        }
        else
        {
            StatusMessage = "No se pudo comunicar con la impresora térmica.";
        }
    }
}
