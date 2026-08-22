using Avalonia;
using Serilog;
using System;
using System.IO;
using System.Threading;
using Velopack;

namespace Ticketfy;

internal static class Program
{
    private const string MutexName = "Global\\Ticketfy_POS_SecureMutex_V1";
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        // 0. Disable SSL globally for Velopack auto-updates on self-signed Forgejo
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

        // 1. Initialize Velopack (crucial for shortcuts, uninstalls, and OTA background updates)
        VelopackApp.Build().Run();

        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            try
            {
                var currentProc = System.Diagnostics.Process.GetCurrentProcess();
                foreach (var p in System.Diagnostics.Process.GetProcessesByName(currentProc.ProcessName))
                {
                    if (p.Id != currentProc.Id)
                    {
                        p.Kill();
                    }
                }
            }
            catch { }
        }

        try
        {
            var dsn = Environment.GetEnvironmentVariable("TICKETFY_SENTRY_DSN");
            
            using (SentrySdk.Init(o =>
            {
                o.Dsn = !string.IsNullOrWhiteSpace(dsn) ? dsn : "https://example@sentry.io/example";
                o.TracesSampleRate = 1.0;
                o.AutoSessionTracking = true;
            }))
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ticketfy", "Logs");
                Directory.CreateDirectory(logDir);

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        Path.Combine(logDir, "ticketfy_.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                try
                {
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    if (ex.GetType().Name != "TaskCanceledException")
                        Log.Fatal(ex, "TICKETFY! crashed fatally");
                    throw;
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
        }
        finally
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
