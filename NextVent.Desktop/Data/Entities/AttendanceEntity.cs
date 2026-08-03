using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// Employee attendance record with optional photo evidence.
/// Maps to legacy 'asistencias' SQLite table.
/// </summary>
[Table("asistencias")]
public class AttendanceEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("usuario_id")]
    public string UsuarioId { get; set; } = string.Empty;

    /// <summary>"ENTRADA" or "SALIDA".</summary>
    [Required]
    [Column("tipo_movimiento")]
    public string TipoMovimiento { get; set; } = "ENTRADA";

    [Required]
    [Column("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [Column("ruta_foto_evidencia")]
    public string? RutaFotoEvidencia { get; set; }

    /// <summary>Navigation: associated user.</summary>
    [ForeignKey(nameof(UsuarioId))]
    public UserEntity? Usuario { get; set; }
}
