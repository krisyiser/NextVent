using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

[Table("item_kits")]
public class ItemKitEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("kit_barcode")]
    public string KitBarcode { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("price")]
    public double Price { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

[Table("item_kit_items")]
public class ItemKitItemEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("item_kit_id")]
    public string ItemKitId { get; set; } = string.Empty;

    [Required]
    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [Column("quantity")]
    public double Quantity { get; set; }
}
