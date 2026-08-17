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
