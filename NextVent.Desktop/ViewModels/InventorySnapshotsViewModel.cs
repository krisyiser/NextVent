using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextVent.Data.Entities;
using NextVent.Services.Implementations;

namespace NextVent.ViewModels;

public partial class InventorySnapshotsViewModel : ObservableObject
{
    private readonly InventorySnapshotService _snapshotService;

    [ObservableProperty]
    private ObservableCollection<InventorySnapshotEntity> _snapshots = new();

    [ObservableProperty]
    private InventorySnapshotEntity? _selectedSnapshot;

    [ObservableProperty]
    private bool _isLoading;

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
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task PrintSnapshotAsync()
    {
        if (SelectedSnapshot == null) return;
        
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var vm = new NextVent.ViewModels.Dialogs.PrintPreviewWindowViewModel($"Reporte de Captura Física: {SelectedSnapshot.CreatedAt:g}");
        var win = new NextVent.Views.Dialogs.PrintPreviewWindow { DataContext = vm };
        
        var confirmed = await win.ShowDialog<bool>(desktop.MainWindow);
        if (!confirmed) return;

        var printerSvc = new NextVent.Services.Implementations.EscPosPrinterService();
        await printerSvc.PrintSnapshotChecklistAsync(SelectedSnapshot);
    }
}
