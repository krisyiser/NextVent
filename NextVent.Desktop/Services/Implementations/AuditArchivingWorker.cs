using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using Serilog;

namespace NextVent.Services.Implementations;

public class AuditArchivingWorker
{
    private readonly IDbContextFactory<AuditDbContext> _auditContextFactory;

    public AuditArchivingWorker(IDbContextFactory<AuditDbContext> auditContextFactory)
    {
        _auditContextFactory = auditContextFactory;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(() => ExecuteAsync(stoppingToken), stoppingToken);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (now.Hour == 3) // Ejecutar en la madrugada
            {
                try
                {
                    using var context = await _auditContextFactory.CreateDbContextAsync(stoppingToken);
                    var thresholdDate = DateTime.Now.AddDays(-30);
                    string thresholdStr = thresholdDate.ToString("o");

                    var oldLogs = await context.AuditLogs
                        .AsNoTracking()
                        .Where(l => string.Compare(l.Timestamp, thresholdStr) < 0)
                        .ToListAsync(stoppingToken);

                    if (oldLogs.Any())
                    {
                        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string archivesFolder = Path.Combine(appDataFolder, "NextVent", "Archives");
                        if (!Directory.Exists(archivesFolder)) Directory.CreateDirectory(archivesFolder);

                        string csvPath = Path.Combine(archivesFolder, $"audit_logs_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                        var sb = new StringBuilder();
                        sb.AppendLine("Id,Timestamp,ActionType,UserId,Reason,FinancialImpact,EntityName,EntityId");
                        foreach (var log in oldLogs)
                        {
                            sb.AppendLine($"{log.Id},{log.Timestamp},{log.ActionType},{log.UserId},\"{log.Reason?.Replace("\"", "\"\"")}\",{log.FinancialImpact},{log.EntityName},{log.EntityId}");
                        }

                        await File.WriteAllTextAsync(csvPath, sb.ToString(), stoppingToken);
                        
                        await DbResilienceHelper.ExecuteWithRetryAsync(async () =>
                        {
                            await context.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs WHERE Timestamp < {0}", thresholdStr);
                            await context.Database.ExecuteSqlRawAsync("VACUUM;");
                        });

                        Log.Information("Archived {Count} old audit logs to {Path}", oldLogs.Count, csvPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error archiving audit logs in AuditArchivingWorker");
                }
                
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
