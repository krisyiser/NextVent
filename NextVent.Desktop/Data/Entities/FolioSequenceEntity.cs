using System.ComponentModel.DataAnnotations;

namespace NextVent.Data.Entities;

public class FolioSequenceEntity
{
    [Key]
    public string DatePrefix { get; set; } = string.Empty;
    public int LastSequence { get; set; }
}
