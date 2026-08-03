using Microsoft.EntityFrameworkCore;
using NextVent.Core.Helpers;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.Services.Implementations;

public sealed class ProductService : IProductService
{
    private readonly AppDbContext _ctx;

    public ProductService(AppDbContext ctx) => _ctx = ctx;

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var list = await _ctx.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<ProductDto>> GetByCategoryAsync(string category)
    {
        var list = await _ctx.Products
            .AsNoTracking()
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
    {
        var entity = await _ctx.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ProductDto?> GetByIdAsync(string id)
    {
        var entity = await _ctx.Products.FindAsync(id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task AddAsync(ProductDto product)
    {
        _ctx.Products.Add(MapToEntity(product));
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductDto product)
    {
        var entity = await _ctx.Products.FindAsync(product.Id);
        if (entity is null) return;

        entity.Barcode = product.Barcode;
        entity.Name = product.Name;
        entity.Cost = product.Cost;
        entity.Price = product.Price;
        entity.WholesalePrice = product.WholesalePrice;
        entity.WholesaleThreshold = product.WholesaleThreshold;
        entity.Stock = product.Stock;
        entity.Category = product.Category;
        entity.Unit = product.Unit;
        entity.ExpiresSoon = product.ExpiresSoon;
        entity.PointsRewarded = product.PointsRewarded;
        entity.ReorderQuantity = product.ReorderQuantity;
        entity.LocationRack = product.LocationRack;
        entity.ClaveSat = product.ClaveSat;
        entity.UnidadSat = product.UnidadSat;

        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _ctx.Products.FindAsync(id);
        if (entity is not null)
        {
            _ctx.Products.Remove(entity);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task ClearInventoryAsync()
    {
        await _ctx.Products.ExecuteDeleteAsync();
    }

    public async Task BulkSaveAsync(IEnumerable<ProductDto> products)
    {
        foreach (var p in products)
        {
            var existing = await _ctx.Products
                .FirstOrDefaultAsync(e => e.Barcode == p.Barcode && p.Barcode != null);

            if (existing is null)
            {
                _ctx.Products.Add(MapToEntity(p));
            }
            else
            {
                existing.Name = p.Name;
                existing.Cost = p.Cost;
                existing.Price = p.Price;
                existing.WholesalePrice = p.WholesalePrice;
                existing.WholesaleThreshold = p.WholesaleThreshold;
                existing.Stock = p.Stock;
                existing.Category = p.Category;
                existing.Unit = p.Unit;
                existing.PointsRewarded = p.PointsRewarded;
                existing.ReorderQuantity = p.ReorderQuantity;
                existing.LocationRack = p.LocationRack;
                existing.ClaveSat = p.ClaveSat;
                existing.UnidadSat = p.UnidadSat;
            }
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task<List<ProductDto>> SearchFtsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return await GetAllAsync();
        var q = query.Trim().ToLower();
        var list = await _ctx.Products
            .AsNoTracking()
            .Where(p => (p.Barcode != null && EF.Functions.Like(p.Barcode.ToLower(), $"%{q}%")) ||
                        EF.Functions.Like(p.Name.ToLower(), $"%{q}%") ||
                        EF.Functions.Like(p.Category.ToLower(), $"%{q}%"))
            .Take(150)
            .ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<int> ImportFromCsvTextAsync(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent)) return 0;
        var items = new List<ProductDto>();
        using var reader = new StringReader(csvContent);
        string? line;
        bool isHeader = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split([';', ',']);
            if (isHeader)
            {
                isHeader = false;
                if (parts[0].Contains("barcode", StringComparison.OrdinalIgnoreCase) || parts[0].Contains("codigo", StringComparison.OrdinalIgnoreCase)) continue;
            }

            if (parts.Length < 4) continue;
            string barcode = parts[0].Trim();
            string name = parts[1].Trim();
            _ = double.TryParse(parts[2].Trim(), out double cost);
            _ = double.TryParse(parts[3].Trim(), out double price);
            double stock = parts.Length > 4 && double.TryParse(parts[4].Trim(), out double s) ? s : 10.0;
            string category = parts.Length > 5 ? parts[5].Trim() : "General";
            string unit = parts.Length > 6 ? parts[6].Trim() : "pza";

            items.Add(new ProductDto(Guid.NewGuid().ToString(), barcode, name, cost, price, Stock: stock, Category: category, Unit: unit));
        }

        if (items.Count > 0) await BulkSaveAsync(items);
        return items.Count;
    }

    public async Task<int> ImportCsvAsync(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        var text = await File.ReadAllTextAsync(filePath);
        return await ImportFromCsvTextAsync(text);
    }

    private static ProductDto MapToDto(ProductEntity e) => new(
        e.Id, e.Barcode, e.Name, e.Cost, e.Price,
        e.WholesalePrice, e.WholesaleThreshold,
        e.Stock, e.Category, e.Unit, e.ExpiresSoon, e.CreatedAt,
        e.PointsRewarded, e.ReorderQuantity, e.LocationRack, e.ClaveSat, e.UnidadSat, e.MinStock);

    private static ProductEntity MapToEntity(ProductDto d) => new()
    {
        Id = string.IsNullOrEmpty(d.Id) ? IdGenerator.NewProductId() : d.Id,
        Barcode = d.Barcode,
        Name = d.Name,
        Cost = d.Cost,
        Price = d.Price,
        WholesalePrice = d.WholesalePrice,
        WholesaleThreshold = d.WholesaleThreshold,
        Stock = d.Stock,
        MinStock = d.MinStock,
        Category = d.Category,
        Unit = d.Unit,
        ExpiresSoon = d.ExpiresSoon,
        CreatedAt = d.CreatedAt ?? DateTimeOffset.UtcNow.ToString("o"),
        PointsRewarded = d.PointsRewarded,
        ReorderQuantity = d.ReorderQuantity,
        LocationRack = d.LocationRack,
        ClaveSat = d.ClaveSat,
        UnidadSat = d.UnidadSat
    };
}
