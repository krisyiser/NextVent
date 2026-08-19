using Ticketfy.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ticketfy.Services.Interfaces;

public interface IInventorySnapshotService
{
    Task<InventorySnapshotEntity> CreateSnapshotAsync(string notes);
    Task<List<InventorySnapshotEntity>> GetSnapshotsAsync();
    Task<InventorySnapshotEntity?> GetSnapshotDetailsAsync(string snapshotId);
}
