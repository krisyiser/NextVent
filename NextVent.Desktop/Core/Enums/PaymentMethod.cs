namespace NextVent.Core.Enums;

/// <summary>
/// Payment methods supported by the POS system.
/// Maps directly from the legacy TypeScript union: 'Cash' | 'Card' | 'Transfer' | 'Credit'
/// </summary>
public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    Transfer = 2,
    Credit = 3
}
