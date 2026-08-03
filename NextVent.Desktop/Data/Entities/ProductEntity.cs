using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// Product catalog entity. Maps to legacy 'products' SQLite table.
/// Tracks inventory items with cost/price/wholesale tiers and stock levels.
/// </summary>
[Table("products")]
public class ProductEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("barcode")]
    public string? Barcode { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("cost")]
    public double Cost { get; set; }

    [Required]
    [Column("price")]
    public double Price { get; set; }

    [Column("wholesalePrice")]
    public double WholesalePrice { get; set; }

    [Column("wholesaleThreshold")]
    public int WholesaleThreshold { get; set; }

    [Column("stock")]
    public double Stock { get; set; }

    [Required]
    [Column("category")]
    public string Category { get; set; } = "Abarrotes";

    [Required]
    [Column("unit")]
    public string Unit { get; set; } = "Pza";

    [Column("expiresSoon")]
    public int ExpiresSoon { get; set; }

    [Column("minStock")]
    public double MinStock { get; set; }

    [Column("points_rewarded")]
    public double PointsRewarded { get; set; } = 1.0;

    [Column("reorder_quantity")]
    public double ReorderQuantity { get; set; } = 10.0;

    [Column("location_rack")]
    public string LocationRack { get; set; } = "Pasillo 1 - Anaquel A";

    [Column("clave_sat")]
    public string ClaveSat { get; set; } = "50202306";

    [Column("unidad_sat")]
    public string UnidadSat { get; set; } = "H87";

    [Column("created_at")]
    public string? CreatedAt { get; set; }
}
