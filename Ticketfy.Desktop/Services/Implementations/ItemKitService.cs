using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketfy.Services.Implementations;

public class ItemKitService : IItemKitService
{
    private readonly AppDbContext _db;

    public ItemKitService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ItemKitDto>> GetAllAsync()
    {
        try
        {
            var kits = await _db.ItemKits.ToListAsync();
            var allKitItems = await _db.ItemKitItems.ToListAsync();
            var products = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.Name);

            var result = new List<ItemKitDto>();
            foreach (var k in kits)
            {
                var items = allKitItems
                    .Where(ki => ki.ItemKitId == k.Id)
                    .Select(ki => new ItemKitItemDto(
                        ki.Id,
                        ki.ItemKitId,
                        ki.ProductId,
                        products.TryGetValue(ki.ProductId, out var pName) ? pName : "Producto no encontrado",
                        ki.Quantity
                    ))
                    .ToList();

                result.Add(new ItemKitDto(k.Id, k.KitBarcode, k.Name, k.Price, k.Description, items));
            }
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting item kits");
            return [];
        }
    }

    public async Task<ItemKitDto?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        try
        {
            var kit = await _db.ItemKits.FirstOrDefaultAsync(k => k.KitBarcode == barcode.Trim());
            if (kit == null) return null;

            var kitItems = await _db.ItemKitItems.Where(ki => ki.ItemKitId == kit.Id).ToListAsync();
            var products = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.Name);

            var items = kitItems
                .Select(ki => new ItemKitItemDto(
                    ki.Id,
                    ki.ItemKitId,
                    ki.ProductId,
                    products.TryGetValue(ki.ProductId, out var pName) ? pName : "Producto",
                    ki.Quantity
                ))
                .ToList();

            return new ItemKitDto(kit.Id, kit.KitBarcode, kit.Name, kit.Price, kit.Description, items);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error finding item kit by barcode {Barcode}", barcode);
            return null;
        }
    }

    public async Task<bool> SaveAsync(string id, string barcode, string name, double price, string description, List<ItemKitItemDto> items)
    {
        try
        {
            var kitId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
            var existing = await _db.ItemKits.FirstOrDefaultAsync(k => k.Id == kitId);

            if (existing == null)
            {
                existing = new ItemKitEntity
                {
                    Id = kitId,
                    KitBarcode = barcode,
                    Name = name,
                    Price = price,
                    Description = description,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                _db.ItemKits.Add(existing);
            }
            else
            {
                existing.KitBarcode = barcode;
                existing.Name = name;
                existing.Price = price;
                existing.Description = description;

                var oldItems = await _db.ItemKitItems.Where(ki => ki.ItemKitId == kitId).ToListAsync();
                _db.ItemKitItems.RemoveRange(oldItems);
            }

            foreach (var item in items)
            {
                _db.ItemKitItems.Add(new ItemKitItemEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    ItemKitId = kitId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            await _db.SaveChangesAsync();
            Log.Information("Saved ItemKit {Name} ({Barcode}) with {Count} items", name, barcode, items.Count);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving ItemKit {Name}", name);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var kit = await _db.ItemKits.FirstOrDefaultAsync(k => k.Id == id);
            if (kit == null) return false;

            var items = await _db.ItemKitItems.Where(ki => ki.ItemKitId == id).ToListAsync();
            _db.ItemKitItems.RemoveRange(items);
            _db.ItemKits.Remove(kit);
            await _db.SaveChangesAsync();
            Log.Information("Deleted ItemKit {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting ItemKit {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeductKitStockAsync(string kitId, double kitQuantity)
    {
        try
        {
            var kitItems = await _db.ItemKitItems.Where(ki => ki.ItemKitId == kitId).ToListAsync();
            foreach (var item in kitItems)
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product != null)
                {
                    double totalDeduction = item.Quantity * kitQuantity;
                    product.Stock = Math.Max(0, product.Stock - totalDeduction);
                    Log.Information("Deducted {Qty} of Product {Name} for Kit Sale", totalDeduction, product.Name);
                }
            }
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deducting stock for ItemKit {KitId}", kitId);
            return false;
        }
    }
}
