using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ticketfy.Data.Entities;

public class InventorySnapshotEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal TotalValue { get; set; }

    public ICollection<InventorySnapshotItemEntity> Items { get; set; } = new List<InventorySnapshotItemEntity>();
}
