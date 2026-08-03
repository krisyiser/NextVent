using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// Payment record against a customer's outstanding debt.
/// Maps to legacy 'customer_payments' SQLite table.
/// </summary>
[Table("customer_payments")]
public class CustomerPaymentEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [Column("date")]
    public string Date { get; set; } = string.Empty;

    [Column("amount")]
    public double Amount { get; set; }

    /// <summary>Navigation: parent customer.</summary>
    [ForeignKey(nameof(CustomerId))]
    public CustomerEntity? Customer { get; set; }
}
