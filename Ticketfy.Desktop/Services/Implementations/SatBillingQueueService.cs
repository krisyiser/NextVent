using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog;

namespace Ticketfy.Services.Implementations;

public class SatBillingQueueService
{
    private readonly Channel<string> _billingQueue;

    public SatBillingQueueService()
    {
        _billingQueue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions 
        { 
            SingleReader = true 
        });
    }

    public async ValueTask EnqueueSaleForBillingAsync(string saleId)
    {
        await _billingQueue.Writer.WriteAsync(saleId);
        Log.Information("Enqueued sale {SaleId} for SAT billing", saleId);
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(() => ExecuteAsync(stoppingToken), stoppingToken);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var saleId in _billingQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                Log.Information("Processing SAT billing for sale: {SaleId}", saleId);
                // 1. Obtener datos de la BD
                // 2. Parsear XML y firmar con SHA-256
                // 3. Consumir API del PAC
                // 4. Actualizar estado FiscalStatus a 'Stamped'
                await Task.Delay(500, stoppingToken); // Simulated PAC latency
                Log.Information("Successfully processed SAT billing for sale: {SaleId}", saleId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process SAT billing for sale {SaleId}", saleId);
            }
        }
    }
}
