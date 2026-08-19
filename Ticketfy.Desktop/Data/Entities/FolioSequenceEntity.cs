using System.ComponentModel.DataAnnotations;

namespace Ticketfy.Data.Entities;

public class FolioSequenceEntity
{
    [Key]
    public string DatePrefix { get; set; } = string.Empty;
    public int LastSequence { get; set; }
}
