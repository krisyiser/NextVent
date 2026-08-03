using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

[Table("returns")]
public class ReturnEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("original_sale_id")]
    public string OriginalSaleId { get; set; } = string.Empty;

    [Column("cashier_user_id")]
    public string? CashierUserId { get; set; }

    [Column("total_refunded")]
    public double TotalRefunded { get; set; }

    [Column("cogs_reversed")]
    public double CogsReversed { get; set; }

    [Column("refund_method")]
    public string RefundMethod { get; set; } = "Efectivo";

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("s");

    [ForeignKey(nameof(OriginalSaleId))]
    public SaleEntity? OriginalSale { get; set; }
}
