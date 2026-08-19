using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketfy.Data.Entities;

/// <summary>
/// Temporarily parked/paused sale order for later resumption.
/// Items stored as JSON. Maps to legacy 'parked_orders' SQLite table.
/// </summary>
[Table("parked_orders")]
public class ParkedOrderEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("items")]
    public string ItemsJson { get; set; } = "[]";

    [Required]
    [Column("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}
