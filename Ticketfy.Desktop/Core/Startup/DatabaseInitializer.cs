using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Seed;
using Ticketfy.Services.Security;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.Core.Startup;

/// <summary>
/// Handles database directory creation, EF Core migration, PRAGMA WAL settings,
/// raw column patches for SQLite, category seeding, and ISO-8601 date migration.
/// Isolated from UI application lifecycle.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        try
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataFolder, "ticketfy", "Database");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string dbPath = Path.Combine(appFolder, "ticketfy.db");

            string securePassword = SecurityManager.GetMasterKey();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath};Password={securePassword};Cache=Shared;Mode=ReadWriteCreate;")
                .Options;

            using (var context = new AppDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Ensure missing columns/tables exist via Raw Sqlite Connection
            using (var rawConn = new SqliteConnection($"Data Source={dbPath};Password={securePassword};"))
            {
                await rawConn.OpenAsync();
                string[] alterQueries = new[]
                {
                    "ALTER TABLE sales ADD COLUMN status INTEGER NOT NULL DEFAULT 0;",
                    "ALTER TABLE sales ADD COLUMN cancellation_reason TEXT NULL;",
                    "ALTER TABLE sales ADD COLUMN cancellation_date TEXT NULL;",
                    "ALTER TABLE sales ADD COLUMN invoice_id TEXT NULL;",
                    "ALTER TABLE sales ADD COLUMN invoice_status TEXT NULL;",
                    "ALTER TABLE cashups ADD COLUMN type TEXT NOT NULL DEFAULT 'Final';",
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
                                using var insertCmd = rawConn.CreateCommand();
                                insertCmd.CommandText = "INSERT OR IGNORE INTO categories (id, name) VALUES (@id, @name);";
                                insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                                insertCmd.Parameters.AddWithValue("@name", cat);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                catch { }

                await rawConn.CloseAsync();
            }

            // Audit DB Initialization
            var auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
                .UseSqlite($"Data Source={Path.Combine(appFolder, "audit_logs.db")};")
                .Options;
            using (var auditContext = new AuditDbContext(auditOptions))
            {
                await auditContext.Database.EnsureCreatedAsync();
            }

            using (var context = new AppDbContext(options))
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
#if DEBUG
                await DatabaseSeeder.SeedAsync(context);
#endif
                Log.Information($"Database initialized and seeded successfully at {dbPath}");

                // Migrate legacy dates
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
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization or seeding failed");
        }
    }
}
