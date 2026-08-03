using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

[Table("shift_notes")]
public class ShiftNoteEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("cashier_name")]
    public string CashierName { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Required]
    [Column("note_text")]
    public string NoteText { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = "General";

    [Column("is_resolved")]
    public bool IsResolved { get; set; } = false;
}
