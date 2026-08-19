using System;
using System.Reflection;
using Ticketfy.Core.Constants;
using Serilog;

namespace Ticketfy.Core.Helpers;

public static class AppVersionHelper
{
    private static string? _cachedVersion;

    /// <summary>
    /// Dynamically resolves the currently running application version (e.g. "v3.0.16").
    /// Reads Executing Assembly version metadata or AppConstants fallback.
    /// </summary>
    public static string DisplayVersion
    {
        get
        {
            if (_cachedVersion != null) return _cachedVersion;

            try
            {
                var asmVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (asmVersion != null && (asmVersion.Major > 0 || asmVersion.Minor > 0))
                {
                    _cachedVersion = $"v{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";
                    return _cachedVersion;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Assembly version lookup bypassed.");
            }

            _cachedVersion = AppConstants.AppVersion;
            return _cachedVersion;
        }
    }

    public static string FullTitle => $"TICKETFY! {DisplayVersion} — Enterprise Desktop Edition";
}
