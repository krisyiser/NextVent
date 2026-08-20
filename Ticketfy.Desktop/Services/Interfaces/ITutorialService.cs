namespace Ticketfy.Services.Interfaces;

/// <summary>
/// Persists and queries completion state for each named tutorial step.
/// Backed by ISettingsService so state survives across sessions without
/// any additional DB migration.
/// </summary>
public interface ITutorialService
{
    /// <summary>Returns true if the given tutorial step key has been marked as complete.</summary>
    Task<bool> IsStepCompletedAsync(string stepKey);

    /// <summary>Marks a tutorial step as permanently completed.</summary>
    Task MarkStepCompletedAsync(string stepKey);

    /// <summary>Marks every known tutorial step as completed (global Skip All).</summary>
    Task MarkAllCompletedAsync();
}
