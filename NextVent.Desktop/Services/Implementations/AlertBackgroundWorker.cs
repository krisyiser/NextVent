using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Entities;
using Serilog;

namespace NextVent.Services.Implementations;

public class AlertBackgroundWorker
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public AlertBackgroundWorker(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateStockAlertsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error en AlertBackgroundWorker");
            }

            await Task.Delay(_checkInterval, cancellationToken);
        }
    }

    private async Task EvaluateStockAlertsAsync()
    {
        using var _ctx = await _contextFactory.CreateDbContextAsync();

        // Obtener productos con bajo stock que no tengan alertas no resueltas.
        var lowStockProducts = await _ctx.Products
            .Where(p => p.Stock <= p.MinStock)
            .ToListAsync();

        foreach (var product in lowStockProducts)
        {
            bool hasActiveAlert = await _ctx.SystemAlerts
                .AnyAsync(a => a.ProductId == product.Id && !a.IsResolved);

            if (!hasActiveAlert)
            {
                var newAlert = new SystemAlertEntity
                {
                    ProductId = product.Id,
                    Title = "Stock Bajo",
                    Message = $"El producto '{product.Name}' tiene un stock de {product.Stock}, el cual está por debajo del mínimo permitido ({product.MinStock})."
                };

                _ctx.SystemAlerts.Add(newAlert);
            }
        }

        await DbResilienceHelper.ExecuteWithRetryAsync(async () => await _ctx.SaveChangesAsync());
    }
}
