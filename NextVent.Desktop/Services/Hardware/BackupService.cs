using Serilog;

namespace NextVent.Services.Hardware;

/// <summary>
/// Database backup service.
/// Copies app.db asynchronously to local backup directory.
/// Replaces backup_database Rust IPC command.
/// </summary>
public sealed class BackupService
{
    public static async Task<string?> CreateBackupAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var appDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NextVent");
                var dbPath = Path.Combine(appDir, "app.db");

                if (!File.Exists(dbPath)) return null;

                var backupDir = Path.Combine(appDir, "backups");
                Directory.CreateDirectory(backupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"app_backup_{timestamp}.db");

                File.Copy(dbPath, backupPath, overwrite: true);
                Log.Information("Database backup created successfully: {Path}", backupPath);
                return backupPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create database backup");
                return null;
            }
        });
    }
}
