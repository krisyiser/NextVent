using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

[Table("giftcards")]
public class GiftcardEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("card_number")]
    public string CardNumber { get; set; } = string.Empty;

    [Column("balance")]
    public double Balance { get; set; }

    [Column("customer_id")]
    public string? CustomerId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}
