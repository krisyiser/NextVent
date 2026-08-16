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

                // Safely ensure status and cancellation columns exist in SQLite table before EF Core model validation
                using (var rawConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    await rawConn.OpenAsync();
                    string[] alterQueries = new[]
                    {
                        "ALTER TABLE sales ADD COLUMN status INTEGER NOT NULL DEFAULT 0;",
                        "ALTER TABLE sales ADD COLUMN cancellation_reason TEXT NULL;",
                        "ALTER TABLE sales ADD COLUMN cancellation_date TEXT NULL;"
                    };
                    foreach (var q in alterQueries)
                    {
                        try
                        {
                            using var cmd = rawConn.CreateCommand();
                            cmd.CommandText = q;
                            await cmd.ExecuteNonQueryAsync();
                        }
                        catch { }
                    }

                    // Create and seed categories table if missing
                    try
                    {
                        using (var cmd = rawConn.CreateCommand())
                        {
                            cmd.CommandText = "CREATE TABLE IF NOT EXISTS categories (id TEXT PRIMARY KEY, name TEXT NOT NULL UNIQUE);";
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var countCmd = rawConn.CreateCommand())
                        {
                            countCmd.CommandText = "SELECT COUNT(*) FROM categories;";
                            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                            if (count == 0)
                            {
                                string[] defaultCats = new[] { "General", "Abarrotes", "Bebidas", "Farmacia", "Hogar" };
                                foreach (var cat in defaultCats)
                                {
                                    using (var insertCmd = rawConn.CreateCommand())
                                    {
                                        insertCmd.CommandText = "INSERT OR IGNORE INTO categories (id, name) VALUES (@id, @name);";
                                        insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                                        insertCmd.Parameters.AddWithValue("@name", cat);
                                        await insertCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    await rawConn.CloseAsync();
                }

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;")
                    .Options;

                using var context = new AppDbContext(options);
                await context.Database.MigrateAsync();

                var auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
                    .UseSqlite($"Data Source={System.IO.Path.Combine(appFolder, "audit_logs.db")};")
                    .Options;
                using var auditContext = new AuditDbContext(auditOptions);
                await auditContext.Database.EnsureCreatedAsync();
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await DatabaseSeeder.SeedAsync(context);
                Log.Information($"Database initialized and seeded successfully at {dbPath}");

                // Migrate any legacy non-ISO-8601 sale dates in the database
                var legacySales = await context.Sales.Where(s => !s.Date.Contains("T")).ToListAsync();
                if (legacySales.Count > 0)
                {
                    foreach (var sale in legacySales)
                    {
                        if (DateTime.TryParse(sale.Date, out var parsedDate))
                        {
                            var localDt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Local);
                            sale.Date = new DateTimeOffset(localDt).UtcDateTime.ToString("o");
                        }
                    }
                    await context.SaveChangesAsync();
                    Log.Information($"Successfully migrated {legacySales.Count} legacy local-formatted sale dates to ISO-8601.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization or seeding failed");
            }

            string businessName = "NextVent POS";
            string contactEmail = "admin@nextvent.com";
            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NextVent", "Database", "nextvent.db")}")
                    .Options;
                using var tempCtx = new AppDbContext(options);
                var bName = await tempCtx.Settings.FirstOrDefaultAsync(s => s.Key == "BusinessName");
                if (bName != null) businessName = bName.Value;
                
                var bEmail = await tempCtx.Settings.FirstOrDefaultAsync(s => s.Key == "ContactEmail");
                if (bEmail != null) contactEmail = bEmail.Value;
            }
            catch { }

            var licenseService = new NextVent.Services.Implementations.LicenseEnforcementService();
            if (licenseService.IsSystemLocked())
            {
                Log.Warning("System locked: Kill Switch activated due to invalid or missing license.jwt.");
                desktop.MainWindow = new Avalonia.Controls.Window
                {
                    Content = new NextVent.Views.LicenseLockedView { DataContext = new NextVent.ViewModels.LicenseLockedViewModel() },
                    SystemDecorations = Avalonia.Controls.SystemDecorations.None,
                    WindowState = Avalonia.Controls.WindowState.Maximized,
                    Topmost = true
                };
            }
            else
            {
                Log.Information("License validated successfully. Starting normal operation...");
                var deviceReg = new NextVent.Services.Implementations.DeviceRegistrationService();
                _ = deviceReg.PingServerAsync(new NextVent.Services.Implementations.BusinessProfile { BusinessName = businessName, Email = contactEmail });

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
