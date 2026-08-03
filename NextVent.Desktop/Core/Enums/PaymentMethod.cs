namespace NextVent.Core.Enums;

/// <summary>
/// Payment methods supported by the POS system.
/// Supports both Spanish UI values (Efectivo, Tarjeta, Mixto) and legacy English mappings.
/// </summary>
public enum PaymentMethod
{
    Efectivo = 0,
    Tarjeta = 1,
    Mixto = 2,
    Transferencia = 3,
    Credito = 4,

    // Aliases for compatibility
    Cash = 0,
    Card = 1,
    Transfer = 3,
    Credit = 4
}
