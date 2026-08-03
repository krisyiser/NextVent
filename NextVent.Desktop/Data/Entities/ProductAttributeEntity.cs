using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

[Table("product_attributes")]
public class ProductAttributeEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [Column("attribute_name")]
    public string AttributeName { get; set; } = string.Empty;

    [Column("attribute_value")]
    public string AttributeValue { get; set; } = string.Empty;

    [Column("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;
}
