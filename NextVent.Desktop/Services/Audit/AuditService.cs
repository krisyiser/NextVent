using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.Services.Audit;

/// <summary>
/// Immutable, append-only security audit log engine.
/// Exposes NO update or delete methods to maintain tamper-evident logs.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IDbContextFactory<AuditDbContext> _contextFactory;

    public AuditService(IDbContextFactory<AuditDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task LogAsync(AuditLogEntity log)
    {
        try
        {
            if (string.IsNullOrEmpty(log.Id)) log.Id = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(log.Timestamp)) log.Timestamp = DateTime.UtcNow.ToString("s");
            if (string.IsNullOrEmpty(log.TerminalName)) log.TerminalName = Environment.MachineName;

            using var context = await _contextFactory.CreateDbContextAsync();
            context.AuditLogs.Add(log);
            
            await DbResilienceHelper.ExecuteWithRetryAsync(async () => await context.SaveChangesAsync());

            Log.Information("AuditLog [{ActionType}] [{RiskLevel}] Entity: {EntityName}/{EntityId}, User: {UserId}, Impact: {Impact:C}",
                log.ActionType, log.RiskLevel, log.EntityName, log.EntityId, log.UserId, log.FinancialImpact);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist immutable audit log entry");
        }
    }

    public async Task LogAsync(string level, string message, string? meta = null)
    {
        var log = new AuditLogEntity
        {
            UserId = "SYSTEM",
            ActionType = Core.Enums.AuditActionType.ShiftDrawerOpenedManually,
            RiskLevel = level.Equals("Warning", StringComparison.OrdinalIgnoreCase) ? Core.Enums.RiskLevel.Warning : Core.Enums.RiskLevel.Info,
            EntityName = "System",
            Reason = message,
            OldValue = meta ?? string.Empty
        };
        await LogAsync(log);
    }

    public async Task<List<AuditLogEntity>> GetRecentLogsAsync(int limit = 100)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
