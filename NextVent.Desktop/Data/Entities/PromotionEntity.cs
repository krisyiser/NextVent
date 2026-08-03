using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// Promotion rule entity supporting product, category, and multibuy discount types.
/// Maps to legacy 'promotions' SQLite table.
/// </summary>
[Table("promotions")]
public class PromotionEntity
{
[Key]
[Column("id")]
public string Id { get; set; } = string.Empty;

[Required]
[Column("name")]
public string Name { get; set; } = string.Empty;

/// <summary>Rule type: "product", "category", or "multibuy".</summary>
[Required]
[Column("type")]
public string Type { get; set; } = "product";

[Column("targetId")]
public string? TargetId { get; set; }

/// <summary>Discount mode: "percent" or "fixed".</summary>
[Column("discountType")]
public string? DiscountType { get; set; }

[Column("discountValue")]
public double DiscountValue { get; set; }

[Column("buyQty")]
public int BuyQty { get; set; }

[Column("payQty")]
public int PayQty { get; set; }

[Column("isActive")]
public int IsActive { get; set; } = 1;
}
