using Ticketfy.Core.Constants;

namespace Ticketfy.Core.Helpers;

public static class AppVersionHelper
{
    /// <summary>
    /// Returns the current application version string (e.g. "v3.0.77").
    /// </summary>
    public static string DisplayVersion => AppConstants.AppVersion;

    /// <summary>
    /// Full application title string for window titles and about page.
    /// </summary>
    public static string FullTitle => $"TICKETFY! {DisplayVersion} — Enterprise Desktop Edition";
}
