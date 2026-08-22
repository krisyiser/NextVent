using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ticketfy.Data;
using Ticketfy.Data.Seed;
using Ticketfy.Views;
using Ticketfy.ViewModels;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using Microsoft.Extensions.DependencyInjection;
using Ticketfy.Services.Interfaces;
using Ticketfy.Services.Implementations;

namespace Ticketfy;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SetupGlobalExceptionHandling();
    }

    private void SetupGlobalExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogFatalError(e.ExceptionObject as Exception, "AppDomain Unhandled");
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogFatalError(e.Exception, "TaskScheduler Unobserved");
            e.SetObserved();
        };
    }

    private void LogFatalError(Exception? ex, string source)
    {
        if (ex == null) return;

        try
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDirectory = System.IO.Path.Combine(appDataFolder, "ticketfy", "Logs");

            if (!System.IO.Directory.Exists(logDirectory))
            {
                System.IO.Directory.CreateDirectory(logDirectory);
            }

            string logFilePath = System.IO.Path.Combine(logDirectory, "crash_log.txt");
            string errorBlock = $"--- CRASH REPORT: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\n" +
                                $"Source: {source}\n" +
                                $"Message: {ex.Message}\n" +
                                $"StackTrace: {ex.StackTrace}\n\n";

            System.IO.File.AppendAllText(logFilePath, errorBlock);

            Log.Fatal(ex, "Fatal crash caught by black box logger from source {Source}", source);
        }
        catch
        {
            // Fail silently
        }
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Log.Information("TICKETFY! {Version} — Avalonia Native Desktop starting", Ticketfy.Core.Helpers.AppVersionHelper.DisplayVersion);

            this.Services = await Ticketfy.Core.Startup.AppBootstrapper.BootstrapServicesAsync();
            var (businessName, contactEmail) = await Ticketfy.Core.Startup.AppBootstrapper.GetBusinessProfileAsync();

            var licenseService = new Ticketfy.Services.Security.LicenseEnforcementService();
            if (licenseService.IsSystemLocked())
            {
                Log.Warning("System locked: Kill Switch activated due to invalid or missing license.jwt.");
                desktop.MainWindow = new Avalonia.Controls.Window
                {
                    Content = new Ticketfy.Views.LicenseLockedView { DataContext = new Ticketfy.ViewModels.LicenseLockedViewModel() },
                    SystemDecorations = Avalonia.Controls.SystemDecorations.None,
                    WindowState = Avalonia.Controls.WindowState.Maximized,
                    Topmost = true
                };
            }
            else
            {
                Log.Information("License validated successfully. Starting normal operation...");
                var deviceReg = new Ticketfy.Services.Implementations.DeviceRegistrationService();
                _ = deviceReg.PingServerAsync(new Ticketfy.Services.Implementations.BusinessProfile { BusinessName = businessName, Email = contactEmail });

                var splash = new SplashWindow();
                desktop.MainWindow = splash;
                splash.Show();

                await System.Threading.Tasks.Task.Delay(3000);

                var mainWin = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };

                desktop.MainWindow = mainWin;
                mainWin.Show();
                splash.Close();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
