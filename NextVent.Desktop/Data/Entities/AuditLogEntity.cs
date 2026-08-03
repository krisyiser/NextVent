using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// System audit log entry for security and operational tracking.
/// Maps to legacy 'audit_log' SQLite table with autoincrement PK.
/// </summary>
[Table("audit_log")]
public class AuditLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [Required]
    [Column("level")]
    public string Level { get; set; } = "info";

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("meta")]
    public string? Meta { get; set; }
}
