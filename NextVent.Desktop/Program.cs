using Avalonia;
using Serilog;
using System;
using System.IO;
using System.Threading;

namespace NextVent;

internal static class Program
{
    private const string MutexName = "Global\\NextVent_POS_SecureMutex_V1";
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running. Exit silently.
            return;
        }

        try
        {
            var dsn = Environment.GetEnvironmentVariable("NEXTVENT_SENTRY_DSN");
            
            using (SentrySdk.Init(o =>
            {
                o.Dsn = !string.IsNullOrWhiteSpace(dsn) ? dsn : "https://example@sentry.io/example";
                o.TracesSampleRate = 1.0;
                o.AutoSessionTracking = true;
            }))
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NextVent", "logs");
                Directory.CreateDirectory(logDir);

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        Path.Combine(logDir, "nextvent_.log"),
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
