using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;

namespace Ticketfy.Services.Implementations;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _context;

    public PurchaseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PurchaseDto>> GetAllAsync()
    {
        var purchases = await _context.Purchases.AsNoTracking().ToListAsync();
        var items = await _context.PurchaseItems.AsNoTracking().ToListAsync();

        var list = new List<PurchaseDto>();
        foreach (var p in purchases)
        {
            var pItems = items.Where(i => i.PurchaseId == p.Id)
                              .Select(i => new PurchaseItemDto(i.Id, i.PurchaseId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice))
                              .ToList();
            list.Add(new PurchaseDto(p.Id, p.SupplierId, p.SupplierName, p.InvoiceNumber, p.Date, p.TotalCost, p.Notes, pItems));
        }
        return list;
    }

    public async Task<PurchaseDto?> GetByIdAsync(string id)
    {
        var p = await _context.Purchases.FindAsync(id);
        if (p == null) return null;

        var items = await _context.PurchaseItems.Where(i => i.PurchaseId == id).ToListAsync();
        var pItems = items.Select(i => new PurchaseItemDto(i.Id, i.PurchaseId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice)).ToList();
        return new PurchaseDto(p.Id, p.SupplierId, p.SupplierName, p.InvoiceNumber, p.Date, p.TotalCost, p.Notes, pItems);
    }

    public async Task<PurchaseDto> RegisterPurchaseAsync(PurchaseDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var purchaseId = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id;
            double totalCost = 0.0;

            var itemDtos = new List<PurchaseItemDto>();

            foreach (var item in dto.Items)
            {
                if (item.UnitPrice <= 0)
                    throw new InvalidOperationException($"Error de integridad: El costo del producto {item.ProductId} no puede ser menor o igual a $0.00.");

                var itemTotal = item.UnitPrice * item.Quantity;
                totalCost += itemTotal;

                var itemEntity = new PurchaseItemEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PurchaseId = purchaseId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = itemTotal
                };
                _context.PurchaseItems.Add(itemEntity);
                itemDtos.Add(new PurchaseItemDto(itemEntity.Id, purchaseId, item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, itemTotal));

                // Restock inventory product & update cost using Weighted Average Costing Formula
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var currentStock = Math.Max(0.0, product.Stock);
                    var purchaseQty = item.Quantity;
                    var purchasePrice = item.UnitPrice;

                    if (purchasePrice > 0)
                    {
                        var totalQuantity = currentStock + purchaseQty;
                        if (totalQuantity > 0)
                        {
                            var weightedCost = ((currentStock * product.Cost) + (purchaseQty * purchasePrice)) / totalQuantity;
                            product.Cost = Math.Round(weightedCost, 2);
                        }
                    }
                    product.Stock += purchaseQty;
                }
            }

            var purchaseEntity = new PurchaseEntity
            {
                Id = purchaseId,
                SupplierId = dto.SupplierId,
                SupplierName = dto.SupplierName,
                InvoiceNumber = dto.InvoiceNumber,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalCost = totalCost,
                Notes = dto.Notes
            };

            _context.Purchases.Add(purchaseEntity);

            // Inject Cash Outflow Movement to Active Shift Drawer
            var activeShift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsOpen == 1);
            if (activeShift != null)
            {
                var outflow = new ShiftMovementEntity
                {
                    ShiftId = activeShift.Id,
                    MovementType = Ticketfy.Core.Enums.MovementType.CompraEfectivo,
                    Amount = totalCost,
                    IsOutflow = true,
                    Description = $"Compra Proveedor: {dto.SupplierName} - Factura: {dto.InvoiceNumber}",
                    ReferenceId = purchaseId,
                    Timestamp = DateTime.Now.ToString("s")
                };
                _context.ShiftMovements.Add(outflow);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new PurchaseDto(purchaseId, dto.SupplierId, dto.SupplierName, dto.InvoiceNumber, purchaseEntity.Date, totalCost, dto.Notes, itemDtos);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
