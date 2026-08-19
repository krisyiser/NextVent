using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketfy.Data.Entities;

/// <summary>
/// Product co-occurrence frequency for the combo recommendation engine.
/// Composite PK on (ProductoA, ProductoB). Maps to legacy 'co_ocurrencia' table.
/// </summary>
[Table("co_ocurrencia")]
public class CoOccurrenceEntity
{
    [Column("producto_a")]
    public string ProductoA { get; set; } = string.Empty;

    [Column("producto_b")]
    public string ProductoB { get; set; } = string.Empty;

    [Column("frecuencia")]
    public int Frecuencia { get; set; } = 1;
}
