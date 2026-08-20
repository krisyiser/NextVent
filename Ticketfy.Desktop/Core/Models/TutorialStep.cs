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
/// AnchorX/AnchorY are 0.0–1.0 fractions of the overlay panel dimensions,
/// allowing AOT-safe positioning without reflection-based control lookups.
/// </summary>
public record TutorialStep(
    string Title,
    string Description,
    double AnchorX,
    double AnchorY,
    double SpotlightWidth = 80,
    double SpotlightHeight = 80,
    TutorialAnchorSide AnchorSide = TutorialAnchorSide.Right
);
