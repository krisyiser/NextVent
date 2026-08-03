using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// System user entity with RBAC role (ADMIN/GERENTE/CAJERO) and hashed credentials.
/// Maps to legacy 'usuarios' SQLite table.
/// </summary>
[Table("usuarios")]
public class UserEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Role: "ADMIN", "GERENTE", or "CAJERO".</summary>
    [Required]
    [Column("rol")]
    public string Rol { get; set; } = "CAJERO";

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("pin_checador_hash")]
    public string? PinChecadorHash { get; set; }

    [Column("estatus")]
    public int Estatus { get; set; } = 1;

    /// <summary>Navigation: attendance records for this user.</summary>
    public ICollection<AttendanceEntity> Attendances { get; set; } = [];
}
