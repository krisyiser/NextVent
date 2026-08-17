using NextVent.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextVent.Services.Interfaces;

public interface IInventorySnapshotService
{
    Task<InventorySnapshotEntity> CreateSnapshotAsync(string notes);
    Task<List<InventorySnapshotEntity>> GetSnapshotsAsync();
    Task<InventorySnapshotEntity?> GetSnapshotDetailsAsync(string snapshotId);
}
