using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NextVent.Data.Entities;
using System.IO;

namespace NextVent.Services.Implementations;

public class SalesVelocityModel
{
    public string Name { get; set; } = string.Empty;
    public double Stock { get; set; }
    public decimal TotalSold28Days { get; set; }
}

public class PredictiveIntelligenceService : IPredictiveIntelligenceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PredictiveIntelligenceService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ProductDto?> GetTopCorrelatedProductAsync(string sourceProductId, List<string> currentCartProductIds)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Buscar correlaciones, excluyendo lo que YA está en el carrito
        var topMatch = await context.Set<CoOccurrenceEntity>()
            .Where(c => c.ProductoA == sourceProductId && !currentCartProductIds.Contains(c.ProductoB))
            .OrderByDescending(c => c.Frecuencia)
            .Select(c => c.ProductoB)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(topMatch)) return null;

        var product = await context.Products.FindAsync(topMatch);
        if (product != null && product.Stock > 0)
        {
            return new ProductDto(
                product.Id, product.Barcode, product.Name, product.Cost, product.Price,
                product.WholesalePrice, product.WholesaleThreshold,
                product.Stock, product.Category, product.Unit, product.ExpiresSoon, product.CreatedAt,
                product.PointsRewarded, product.ReorderQuantity, product.LocationRack, product.ClaveSat, product.UnidadSat, product.MinStock);
        }
        return null;
    }

    [DapperAot]
    public async Task<List<PredictiveAlertDto>> GetUrgentRestockAlertsAsync()
    {
        var thresholdDate = DateTime.Now.AddDays(-28).ToString("o"); // Ensure format matches SQLite text date
        
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbPath = Path.Combine(appDataFolder, "NextVent", "Database", "nextvent.db");
        string securePassword = NextVent.Services.Security.SecurityManager.GetMasterKey();
        string secureConnectionString = $"Data Source={dbPath};Password={securePassword};";

        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(secureConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                p.Name,
                p.Stock,
                SUM(si.Quantity) as TotalSold28Days
            FROM products p
            JOIN sale_items si ON p.Id = si.ProductId
            JOIN sales s ON si.SaleId = s.Id
            WHERE s.Date >= @ThresholdDate AND s.Status = 0
            GROUP BY p.Id, p.Name, p.Stock
            HAVING SUM(si.Quantity) > 0";

        var stats = await connection.QueryAsync<SalesVelocityModel>(sql, new { ThresholdDate = thresholdDate });

        var alerts = new List<PredictiveAlertDto>();

        foreach (var stat in stats)
        {
            decimal dailyVelocity = stat.TotalSold28Days / 28m;
            if (dailyVelocity <= 0) continue;

            decimal daysRemaining = (decimal)stat.Stock / dailyVelocity;

            if (daysRemaining <= 3.0m) // Límite crítico de 3 días
            {
                alerts.Add(new PredictiveAlertDto 
                {
                    Message = $"🚨 URGENTE: Te quedarás sin {stat.Name} en {Math.Round(daysRemaining, 1)} días a tu ritmo de ventas actual. ¡Pide al proveedor hoy!",
                    DaysRemaining = daysRemaining
                });
            }
        }
        
        return alerts.OrderBy(a => a.DaysRemaining).ToList();
    }
}
