using Serilog;
using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Locators;
using Avalonia.Threading;
using System.Threading;

namespace Ticketfy.Services.Implementations;

public class AutoUpdateService
{
    // Configure the remote URL where your releases (Setup.exe, .nupkg) will be hosted.
    // For Forgejo/Gitea public repositories, you can use the raw file endpoint:
    private const string UpdateUrl = "https://valcore.cloud/downloads"; 

    public bool IsUpdateReadyToInstall { get; private set; }
    public string NewVersion { get; private set; } = string.Empty;
    public double DownloadProgress { get; private set; }

    public event Action? UpdateAvailableEvent;
    public event Action<double>? DownloadProgressChangedEvent;
    public event Action? UpdateReadyToInstallEvent;
    public event Action<string>? UpdateFailedEvent;
    public event Action? UpdateUpToDateEvent;

    public AutoUpdateService()
    {
    }

    /// <summary>
    /// Checks for updates silently in the background. If a new version is found,
    /// it downloads it silently and notifies the UI when it's ready to restart.
    /// </summary>
    public async Task CheckAndDownloadUpdatesAsync()
    {
        try
        {
            Log.Information("Velopack: Checking for OTA updates at {Url}", UpdateUrl);
            var source = new Velopack.Sources.SimpleWebSource(UpdateUrl, new InsecureFileDownloader());
            var mgr = new UpdateManager(source);
            
            if (!mgr.IsInstalled)
            {
                Log.Warning("Velopack: Application is not installed via Velopack setup. OTA updates disabled in development mode.");
                return;
            }

            var newVersionInfo = await mgr.CheckForUpdatesAsync();
            if (newVersionInfo == null)
            {
                Log.Information("Velopack: No updates found. System is up to date.");
                Dispatcher.UIThread.Post(() => UpdateUpToDateEvent?.Invoke());
                return;
            }

            NewVersion = newVersionInfo.TargetFullRelease.Version.ToString();
            Log.Information("Velopack: New version found: v{Version}. Starting background download...", NewVersion);
            
            Dispatcher.UIThread.Post(() => UpdateAvailableEvent?.Invoke());

            // Download the update in the background
            await mgr.DownloadUpdatesAsync(newVersionInfo, progress => 
            {
                DownloadProgress = progress;
                Dispatcher.UIThread.Post(() => DownloadProgressChangedEvent?.Invoke(progress));
            });

            Log.Information("Velopack: Update downloaded successfully. Ready to apply on restart.");
            IsUpdateReadyToInstall = true;
            Dispatcher.UIThread.Post(() => UpdateReadyToInstallEvent?.Invoke());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Velopack: Failed to check or download updates.");
            Dispatcher.UIThread.Post(() => UpdateFailedEvent?.Invoke(ex.Message));
        }
    }

    /// <summary>
    /// Applies the downloaded update and restarts the application immediately.
    /// </summary>
    public void ApplyUpdatesAndRestart()
    {
        try
        {
            if (!IsUpdateReadyToInstall) return;

            Log.Information("Velopack: Applying OTA update and restarting...");
            var source = new Velopack.Sources.SimpleWebSource(UpdateUrl, new InsecureFileDownloader());
            var mgr = new UpdateManager(source);
            var pendingAsset = mgr.UpdatePendingRestart;
            if (pendingAsset != null)
            {
                mgr.ApplyUpdatesAndRestart(pendingAsset);
            }
            else
            {
                Log.Warning("Velopack: Could not find any pending updates to apply.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Velopack: Failed to apply updates and restart.");
        }
    }

    /// <summary>
    /// Starts a background timer that checks for updates periodically (e.g. every 6 hours).
    /// </summary>
    public void StartPeriodicChecks(TimeSpan interval)
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await CheckAndDownloadUpdatesAsync();
                await Task.Delay(interval);
            }
        });
    }
}

public class InsecureFileDownloader : Velopack.Sources.HttpClientFileDownloader
{
    protected override System.Net.Http.HttpClientHandler CreateHttpClientHandler()
    {
        var handler = base.CreateHttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return handler;
    }
}

