namespace Ticketfy.Core.Enums;

public enum AttendanceStatus
{
    Active,              // Checked in, currently working
    Completed,           // Successfully checked out
    IncompleteAnomaly,   // System auto-closed due to forgotten checkout
    ShiftMismatch        // Flagged: Shift closed but attendance remained open > 24h
}
