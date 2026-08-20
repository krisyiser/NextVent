namespace Ticketfy.Core.Models;

/// <summary>
/// Defines which side of the spotlight anchor the coach tooltip is rendered on.
/// </summary>
public enum TutorialAnchorSide
{
    Right,
    Left,
    Bottom,
    Top
}

/// <summary>
/// Represents a single step in a guided tutorial tour.
/// TargetName specifies the x:Name of the target Avalonia control to highlight precisely.
/// If TargetName is omitted or not found, AnchorX/AnchorY fractions are used as fallback.
/// </summary>
public record TutorialStep(
    string Title,
    string Description,
    string? TargetName = null,
    double AnchorX = 0.5,
    double AnchorY = 0.5,
    double SpotlightWidth = 80,
    double SpotlightHeight = 80,
    TutorialAnchorSide AnchorSide = TutorialAnchorSide.Right
);
