using System;
using System.IO;
using System.Threading.Tasks;
using NextVent.Services.Interfaces;

namespace NextVent.Services.Implementations;

public class BackupService : IBackupService
{
    private readonly string _dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
    private readonly string _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

    public async Task<bool> CreateZCutBackupAsync(string shiftReference)
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"backup_ZCut_{shiftReference}_{timestamp}.db";
            string destinationPath = Path.Combine(_backupDirectory, backupFileName);

            // Using FileShare.ReadWrite to copy safely even if WAL mode is active
            using (var sourceStream = new FileStream(_dbFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error creating Z-Cut backup: {Message}", ex.Message);
            return false;
        }
    }
}
