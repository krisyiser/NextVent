using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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
}
