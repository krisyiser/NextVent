namespace Ticketfy.Core.Enums;

/// <summary>
/// Role-based access control tiers for the POS system.
/// Determines which actions require manager override (CAJERO restricted).
/// </summary>
public enum UserRole
{
    Admin = 0,
    Gerente = 1,
    Cajero = 2
}
