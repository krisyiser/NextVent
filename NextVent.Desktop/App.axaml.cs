using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NextVent.Data;
using NextVent.Data.Seed;
using NextVent.Views;
using NextVent.ViewModels;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;

namespace NextVent;

public partial class App : Application
{
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
            string logDirectory = System.IO.Path.Combine(appDataFolder, "NextVent", "Logs");

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
            Log.Information("NextVent POS v3.0 — Avalonia Native Desktop starting");

            try
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appFolder = System.IO.Path.Combine(appDataFolder, "NextVent", "Database");
                if (!System.IO.Directory.Exists(appFolder))
                {
                    System.IO.Directory.CreateDirectory(appFolder);
                }
                string dbPath = System.IO.Path.Combine(appFolder, "nextvent.db");
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Journal Mode=WAL;")
                    .Options;

                using var context = new AppDbContext(options);
                await context.Database.MigrateAsync();
                await DatabaseSeeder.SeedAsync(context);
                Log.Information($"Database initialized and seeded successfully at {dbPath}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization or seeding failed");
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
