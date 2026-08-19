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

            try
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appFolder = System.IO.Path.Combine(appDataFolder, "ticketfy", "Database");
                if (!System.IO.Directory.Exists(appFolder))
                {
                    System.IO.Directory.CreateDirectory(appFolder);
                }
                string dbPath = System.IO.Path.Combine(appFolder, "ticketfy.db");

                string securePassword = Ticketfy.Services.Security.SecurityManager.GetMasterKey();
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath};Password={securePassword};Cache=Shared;Mode=ReadWriteCreate;")
                    .Options;

                using var context = new AppDbContext(options);
                await context.Database.EnsureCreatedAsync();

                // Safely ensure status and cancellation columns exist in SQLite table before EF Core model validation
                using (var rawConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Password={securePassword};"))
                {
                    await rawConn.OpenAsync();
                    string[] alterQueries = new[]
                    {
                        "ALTER TABLE sales ADD COLUMN status INTEGER NOT NULL DEFAULT 0;",
                        "ALTER TABLE sales ADD COLUMN cancellation_reason TEXT NULL;",
                        "ALTER TABLE sales ADD COLUMN cancellation_date TEXT NULL;",
                        "ALTER TABLE sales ADD COLUMN invoice_id TEXT NULL;",
                        "ALTER TABLE sales ADD COLUMN invoice_status TEXT NULL;",
                        "CREATE TABLE IF NOT EXISTS InventorySnapshots (Id TEXT PRIMARY KEY, CreatedAt TEXT NOT NULL, Notes TEXT NOT NULL, TotalItems INTEGER NOT NULL, TotalValue TEXT NOT NULL);",
                        "CREATE TABLE IF NOT EXISTS InventorySnapshotItems (Id TEXT PRIMARY KEY, SnapshotId TEXT NOT NULL, ProductId TEXT NOT NULL, Barcode TEXT, Name TEXT NOT NULL, Quantity TEXT NOT NULL, CostPrice TEXT NOT NULL, SellingPrice TEXT NOT NULL, FOREIGN KEY(SnapshotId) REFERENCES InventorySnapshots(Id));"
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

                var auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
                    .UseSqlite($"Data Source={System.IO.Path.Combine(appFolder, "audit_logs.db")};")
                    .Options;
                using var auditContext = new AuditDbContext(auditOptions);
                await auditContext.Database.EnsureCreatedAsync();
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
#if DEBUG
                await DatabaseSeeder.SeedAsync(context);
#endif
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

            string businessName = "TICKETFY!";
            string contactEmail = "admin@ticketfy.com";
            try
            {
                string securePassword = Ticketfy.Services.Security.SecurityManager.GetMasterKey();
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ticketfy", "Database", "ticketfy.db")};Password={securePassword};")
                    .Options;
                using var tempCtx = new AppDbContext(options);
                var bName = await tempCtx.Settings.FirstOrDefaultAsync(s => s.Key == "EmpresaNombreComercial");
                if (bName != null && !string.IsNullOrWhiteSpace(bName.Value)) businessName = bName.Value;
                
                var bEmail = await tempCtx.Settings.FirstOrDefaultAsync(s => s.Key == "ContactEmail");
                if (bEmail != null && !string.IsNullOrWhiteSpace(bEmail.Value)) contactEmail = bEmail.Value;
            }
            catch { }

            var services = new ServiceCollection();

            services.AddSingleton<IFacturamaService>(sp => new FacturamaService(new System.Net.Http.HttpClient()));

            this.Services = services.BuildServiceProvider();

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
