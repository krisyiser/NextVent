using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextVent.Data.Entities;

/// <summary>
/// Customer entity with debt tracking and loyalty points.
/// Maps to legacy 'customers' SQLite table.
/// </summary>
[Table("customers")]
public class CustomerEntity
{
[Key]
[Column("id")]
public string Id { get; set; } = string.Empty;

[Required]
[Column("name")]
public string Name { get; set; } = string.Empty;

[Column("phone")]
public string Phone { get; set; } = string.Empty;

[Column("customer_code")]
public string CustomerCode { get; set; } = string.Empty;

[Column("rfc")]
public string Rfc { get; set; } = string.Empty;

[Column("email")]
public string Email { get; set; } = string.Empty;

[Column("address")]
public string Address { get; set; } = string.Empty;

[Column("credit_limit")]
public double CreditLimit { get; set; } = 5000.0;

[Column("debt")]
public double Debt { get; set; }

[Column("puntos_saldo")]
public double PuntosSaldo { get; set; }

/// <summary>Navigation: payments made by this customer.</summary>
public ICollection<CustomerPaymentEntity> Payments { get; set; } = [];

/// <summary>Navigation: optional fiscal data link.</summary>
public FiscalClientEntity? FiscalData { get; set; }
}
