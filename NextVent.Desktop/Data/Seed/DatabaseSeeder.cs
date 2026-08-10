using Microsoft.EntityFrameworkCore;
using NextVent.Core.Helpers;
using NextVent.Data.Entities;
using System;
using System.Threading.Tasks;

namespace NextVent.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTimeOffset.UtcNow.ToString("o");

        // 1. Seed/Upsert 25 Varied Test Products unconditionally
        var seedList = new[]
        {
            new ProductEntity { Id = "PROD-SEED-001", Barcode = "7501234567890", Name = "Coca-Cola 600ml", Cost = 12.5, Price = 20.0, WholesalePrice = 18.0, Stock = 45, Category = "Bebidas", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-002", Barcode = "7509876543210", Name = "Leche Lala Entera 1L", Cost = 22.0, Price = 28.0, Stock = 12, Category = "Lácteos", Unit = "Pza", ExpiresSoon = 1, CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-003", Barcode = "7501000000003", Name = "Pan Bimbo Blanco Rendidor", Cost = 34.0, Price = 45.0, Stock = 18, Category = "Panadería", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-004", Barcode = "7502000000004", Name = "Paracetamol 500mg (20 Tab)", Cost = 18.0, Price = 35.0, Stock = 68, Category = "Farmacia", Unit = "Caja", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-005", Barcode = "7503000000005", Name = "Sabritas Saladas 40g", Cost = 10.0, Price = 16.0, Stock = 50, Category = "Botanas", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-006", Barcode = "7504000000006", Name = "Huevo San Juan (Kilo)", Cost = 32.0, Price = 42.0, Stock = 30, Category = "Abarrotes", Unit = "Kg", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-007", Barcode = "7505000000007", Name = "Gatorade Naranja 1L", Cost = 21.0, Price = 29.0, Stock = 24, Category = "Bebidas", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-008", Barcode = "7506000000008", Name = "Aspirina Protect 100mg", Cost = 38.0, Price = 55.0, Stock = 15, Category = "Farmacia", Unit = "Caja", ExpiresSoon = 1, CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-009", Barcode = "7507000000009", Name = "Jabón Zote Rosa 400g", Cost = 14.0, Price = 22.0, Stock = 40, Category = "Limpieza", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-010", Barcode = "7508000000010", Name = "Fabuloso Lavanda 1L", Cost = 23.0, Price = 32.0, Stock = 18, Category = "Limpieza", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-011", Barcode = "7509000000011", Name = "Agua Ciel 1.5L", Cost = 8.5, Price = 15.0, Stock = 60, Category = "Bebidas", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-012", Barcode = "7509000000012", Name = "Galletas Marías Gamesa 170g", Cost = 13.0, Price = 19.5, Stock = 35, Category = "Abarrotes", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-013", Barcode = "7509000000013", Name = "Queso Panela Nochebuena 400g", Cost = 48.0, Price = 68.0, Stock = 14, Category = "Lácteos", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-014", Barcode = "7509000000014", Name = "Café Nescafé Clásico 120g", Cost = 52.0, Price = 72.0, Stock = 22, Category = "Abarrotes", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-015", Barcode = "7509000000015", Name = "Atún Herdez en Agua 130g", Cost = 14.5, Price = 21.0, Stock = 40, Category = "Abarrotes", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-016", Barcode = "7509000000016", Name = "Cloralex El Rendidor 950ml", Cost = 11.0, Price = 18.5, Stock = 25, Category = "Limpieza", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-017", Barcode = "7509000000017", Name = "Papel Higiénico Pétalo 4 Rollos", Cost = 22.0, Price = 34.0, Stock = 28, Category = "Limpieza", Unit = "Paq", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-018", Barcode = "7509000000018", Name = "Chocolate Carlos V 18g", Cost = 7.0, Price = 12.0, Stock = 80, Category = "Dulcería", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-019", Barcode = "7509000000019", Name = "Manzana Golden (Kilo)", Cost = 24.0, Price = 38.0, Stock = 15, Category = "Frutas y Verduras", Unit = "Kg", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-020", Barcode = "7509000000020", Name = "Aguacate Hass (Kilo)", Cost = 50.0, Price = 75.0, Stock = 10, Category = "Frutas y Verduras", Unit = "Kg", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-021", Barcode = "7509000000021", Name = "Pechuga de Pollo Fresca (Kg)", Cost = 85.0, Price = 110.0, Stock = 8, Category = "Carnicería", Unit = "Kg", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-022", Barcode = "7509000000022", Name = "Cerveza Corona Extra 355ml", Cost = 16.0, Price = 24.0, Stock = 72, Category = "Bebidas", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-023", Barcode = "7509000000023", Name = "Crema Lala Ácida 450g", Cost = 21.5, Price = 31.0, Stock = 16, Category = "Lácteos", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-024", Barcode = "7509000000024", Name = "Peñafiel Naranjada 2L", Cost = 18.0, Price = 27.0, Stock = 20, Category = "Bebidas", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-025", Barcode = "7509000000025", Name = "Electrolit Suero Fresa 625ml", Cost = 24.0, Price = 36.0, Stock = 33, Category = "Farmacia", Unit = "Pza", CreatedAt = now },
            new ProductEntity { Id = "PROD-SEED-026", Barcode = "7509000000026", Name = "Jamón Cocido (Kilo)", Cost = 120.0, Price = 180.0, Stock = 15, Category = "Carnicería", Unit = "Kg", CreatedAt = now }
        };

        foreach (var p in seedList)
        {
            var existing = await context.Products.FindAsync(p.Id);
            if (existing == null)
            {
                context.Products.Add(p);
            }
            else
            {
                existing.Name = p.Name;
                existing.Barcode = p.Barcode;
                existing.Price = p.Price;
                existing.WholesalePrice = p.WholesalePrice;
                existing.WholesaleThreshold = p.WholesaleThreshold;
                existing.Cost = p.Cost;
                existing.Stock = p.Stock;
                existing.Category = p.Category;
                existing.Unit = p.Unit;
            }
        }
        await context.SaveChangesAsync();

        // 2. User seeding is now delegated to OOBE FirstTimeSetupView if empty
        // context.Users.AddRange(...) removed to allow OOBE setup.

        // 3. Seed Sample Customers if empty
        if (!await context.Customers.AnyAsync())
        {
            context.Customers.AddRange(
                new CustomerEntity
                {
                    Id = "CUST-SEED-001",
                    Name = "Público en General",
                    Phone = "0000000000",
                    Debt = 0,
                    PuntosSaldo = 0
                },
                new CustomerEntity
                {
                    Id = "CUST-SEED-002",
                    Name = "Juan Pérez (Cliente Frecuente)",
                    Phone = "5551234567",
                    Debt = 150.00,
                    PuntosSaldo = 45
                },
                new CustomerEntity
                {
                    Id = "CUST-SEED-003",
                    Name = "Distribuidora del Norte (Mayorista)",
                    Phone = "5559876543",
                    Debt = 0,
                    PuntosSaldo = 0
                }
            );
        }

        // 4. Seed Promotions if empty
        if (!await context.Promotions.AnyAsync())
        {
            context.Promotions.AddRange(
                new PromotionEntity { Id = "PROM-001", Name = "10% de Descuento en Bebidas", DiscountValue = 10, IsActive = 1 },
                new PromotionEntity { Id = "PROM-002", Name = "Oferta Especial 2x1 Abarrotes", DiscountValue = 15, IsActive = 1 },
                new PromotionEntity { Id = "PROM-003", Name = "Descuento Preferencial Cliente Frecuente", DiscountValue = 5, IsActive = 1 }
            );
        }

        await context.SaveChangesAsync();
    }
}
