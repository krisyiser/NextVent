using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextVent.Core.Enums;

namespace NextVent.Data.Entities;

[Table("shift_movements")]
public class ShiftMovementEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("shift_id")]
    public string ShiftId { get; set; } = string.Empty;

    [Column("movement_type")]
    public MovementType MovementType { get; set; } = MovementType.AbonoCliente;

    [Column("amount")]
    public double Amount { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("s");

    [ForeignKey(nameof(ShiftId))]
    public ShiftEntity? Shift { get; set; }
}
