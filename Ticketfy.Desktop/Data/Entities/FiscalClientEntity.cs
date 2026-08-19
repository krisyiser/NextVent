using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketfy.Data.Entities;

/// <summary>
/// Mexican fiscal client data for CFDI 4.0 invoice generation.
/// Maps to legacy 'clientes_fiscales' SQLite table.
/// </summary>
[Table("clientes_fiscales")]
public class FiscalClientEntity
{
    /// <summary>Same ID as the parent CustomerEntity for 1:1 mapping.</summary>
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("rfc")]
    public string Rfc { get; set; } = string.Empty;

    [Required]
    [Column("razon_social")]
    public string RazonSocial { get; set; } = string.Empty;

    [Required]
    [Column("codigo_postal")]
    public string CodigoPostal { get; set; } = string.Empty;

    [Required]
    [Column("regimen_fiscal")]
    public string RegimenFiscal { get; set; } = string.Empty;

    [Required]
    [Column("uso_cfdi")]
    public string UsoCfdi { get; set; } = string.Empty;

    /// <summary>Navigation: parent customer.</summary>
    [ForeignKey(nameof(Id))]
    public CustomerEntity? Customer { get; set; }
}
