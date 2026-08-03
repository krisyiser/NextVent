using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// Cash register shift tracking entity.
/// Tracks opening/closing balances and cash flow discrepancies.
/// Maps to legacy 'shifts' SQLite table.
/// </summary>
[Table("shifts")]
public class ShiftEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [Column("endTime")]
    public string? EndTime { get; set; }

    [Column("openingBalance")]
    public double OpeningBalance { get; set; }

    [Column("totalCashSales")]
    public double TotalCashSales { get; set; }

    [Column("totalCreditSales")]
    public double TotalCreditSales { get; set; }

    [Column("expectedBalance")]
    public double ExpectedBalance { get; set; }

    [Column("actualBalance")]
    public double? ActualBalance { get; set; }

    [Column("diff")]
    public double? Diff { get; set; }

    [Column("isOpen")]
    public int IsOpen { get; set; } = 1;
}
