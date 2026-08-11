using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextVent.Core.Enums;

namespace NextVent.Data.Entities;

/// <summary>
/// Sale transaction entity. Items are stored as serialized JSON in ItemsJson.
/// Maps to legacy 'sales' SQLite table with fiscal timbrado columns.
/// </summary>
[Table("sales")]
public class SaleEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>JSON-serialized array of SaleItemSnapshot records.</summary>
    [Required]
    [Column("items")]
    public string ItemsJson { get; set; } = "[]";

    [Column("total")]
    public double Total { get; set; }

    [Column("totalCost")]
    public double TotalCost { get; set; }

    [Column("profit")]
    public double Profit { get; set; }

    [Column("paidAmount")]
    public double PaidAmount { get; set; }

    [Column("changeAmount")]
    public double ChangeAmount { get; set; }

    [Required]
    [Column("paymentMethod")]
    public string PaymentMethod { get; set; } = "Cash";

    [Column("customerId")]
    public string? CustomerId { get; set; }

    [Column("isCredit")]
    public int IsCredit { get; set; }

    [Column("isCancelled")]
    public int IsCancelled { get; set; }

    [Column("cancelledAt")]
    public string? CancelledAt { get; set; }

    [Column("estado_fiscal")]
    public string EstadoFiscal { get; set; } = "PENDIENTE";

    [Column("uuid_sat")]
    public string? UuidSat { get; set; }

    [Column("serie_folio")]
    public string? SerieFolio { get; set; }

    [Column("status")]
    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    [Column("cancellation_date")]
    public string? CancellationDate { get; set; }
}
