using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketfy.Data.Entities;

[Table("system_alerts")]
public class SystemAlertEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("product_id")]
    public string? ProductId { get; set; }

    [Column("supplier_id")]
    public string? SupplierId { get; set; }

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.Now.ToString("s");

    [Column("is_resolved")]
    public bool IsResolved { get; set; } = false;

    [ForeignKey(nameof(ProductId))]
    public ProductEntity? Product { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public SupplierEntity? Supplier { get; set; }
}
