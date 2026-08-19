using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextVent.Core.Enums;

namespace NextVent.Data.Entities;

[Table("asistencias")]
public partial class AttendanceEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("usuario_id")]
    public Guid UserId { get; set; }

    [Column("check_in_time")]
    public DateTime CheckInTime { get; set; } = DateTime.Now;

    [Column("check_out_time")]
    public DateTime? CheckOutTime { get; set; }

    [Column("status")]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Active;

    [Column("terminal_name")]
    public string TerminalName { get; set; } = Environment.MachineName;

    [Column("notes")]
    public string Notes { get; set; } = string.Empty;

    [NotMapped]
    public double TotalWorkedHours => CheckOutTime.HasValue
        ? (CheckOutTime.Value - CheckInTime).TotalHours
        : (DateTime.Now - CheckInTime).TotalHours;

    [ForeignKey(nameof(UserId))]
    public UserEntity? Usuario { get; set; }
}
