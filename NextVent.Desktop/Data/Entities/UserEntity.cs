using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextVent.Core.Enums;

namespace NextVent.Data.Entities;

/// <summary>
/// System user entity with secure credentials and RBAC mapping.
/// Maps to legacy 'usuarios' SQLite table.
/// </summary>
[Table("usuarios")]
public partial class UserEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("nombre")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("password_hint")]
    public string PasswordHint { get; set; } = string.Empty; // Pista de contraseña

    [Column("pin_checador_hash")]
    public string PinCode { get; set; } = string.Empty;      // 4-digit fast PIN

    [Column("rol")]
    public UserRole Role { get; set; } = UserRole.Cajero;

    [Column("estatus")]
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation: attendance records for this user.</summary>
    public ICollection<AttendanceEntity> Attendances { get; set; } = [];
}
