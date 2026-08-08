using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Implementations;
using Xunit;

namespace NextVent.Desktop.Tests;

public sealed class SaleServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly SaleService _saleService;
    private readonly ProductService _productService;
    private readonly CustomerService _customerService;

    public SaleServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _saleService = new SaleService(_context);
        _productService = new ProductService(_context);
        _customerService = new CustomerService(_context);
    }

    [Fact]
    public async Task SaveAsync_ShouldDeductStockAndRecordSale()
    {
        // 1. Setup Product with stock = 20
        var product = new ProductDto("PROD-101", "12345", "Chips", 5.0, 10.0, 8.0, 5, 20, "Botanas", "Pza", 0, null);
        await _productService.AddAsync(product);

        // 2. Execute Sale of 3 items
        var itemSnapshot = new SaleItemSnapshotDto("PROD-101", "Chips", 10.0, 5.0, 3, "Pza", "Botanas", 0.0, 30.0);
        var saleDto = new SaleDto(
            Id: "SALE-101",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [itemSnapshot],
            Total: 30.0,
            TotalCost: 15.0,
            Profit: 15.0,
            PaidAmount: 50.0,
            ChangeAmount: 20.0,
            PaymentMethod: "Cash",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        var savedSale = await _saleService.SaveAsync(saleDto);

        // 3. Verify Sale saved and Stock deducted (20 - 3 = 17)
        Assert.NotNull(savedSale);
        var updatedProduct = await _productService.GetByIdAsync("PROD-101");
        Assert.NotNull(updatedProduct);
        Assert.Equal(17.0, updatedProduct.Stock);
    }

    [Fact]
    public async Task CreditSale_ShouldIncreaseCustomerDebt()
    {
        // Setup Customer with debt = 0
        var customer = new CustomerDto("CUST-101", "Ana López", "5550001111", "ana@ana.com", "RFC123", 1000.0, 0.0);
        await _customerService.AddAsync(customer);

        var product = new ProductDto("PROD-102", "999", "Milk", 15.0, 25.0, 0, 0, 10, "Lácteos", "Pza", 0, null);
        await _productService.AddAsync(product);

        // Execute Credit Sale of total 50.0
        var itemSnapshot = new SaleItemSnapshotDto("PROD-102", "Milk", 25.0, 15.0, 2, "Pza", "Lácteos", 0.0, 50.0);
        var creditSale = new SaleDto(
            Id: "SALE-CREDIT-01",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [itemSnapshot],
            Total: 50.0,
            TotalCost: 30.0,
            Profit: 20.0,
            PaidAmount: 0.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Credit",
            CustomerId: "CUST-101",
            IsCredit: true,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        await _saleService.SaveAsync(creditSale);

        // Verify Customer Debt increased to 50.0
        var updatedCustomer = await _customerService.GetByIdAsync("CUST-101");
        Assert.NotNull(updatedCustomer);
        Assert.Equal(50.0, updatedCustomer.Debt);
    }

    [Fact]
    public async Task CancelSale_ShouldRestoreStockAndReduceCreditDebt()
    {
        // Setup Customer debt = 100, Product stock = 5
        var customer = new CustomerDto("CUST-102", "Pedro", "5552223333", "pedro@pedro.com", "RFC123", 1000.0, 0.0);
        await _customerService.AddAsync(customer);

        var product = new ProductDto("PROD-103", "888", "Juice", 10.0, 20.0, 0, 0, 10, "Bebidas", "Pza", 0, null);
        await _productService.AddAsync(product);

        var itemSnapshot = new SaleItemSnapshotDto("PROD-103", "Juice", 20.0, 10.0, 5, "Pza", "Bebidas", 0.0, 100.0);
        var saleDto = new SaleDto(
            Id: "SALE-CANCEL-TEST",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [itemSnapshot],
            Total: 100.0,
            TotalCost: 50.0,
            Profit: 50.0,
            PaidAmount: 0.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Credit",
            CustomerId: "CUST-102",
            IsCredit: true,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        await _saleService.SaveAsync(saleDto);

        // Verify Stock is 5 and Debt is 100 before cancellation
        var prodBeforeCancel = await _productService.GetByIdAsync("PROD-103");
        Assert.Equal(5.0, prodBeforeCancel!.Stock);

        // Cancel Sale
        await _saleService.CancelAsync("SALE-CANCEL-TEST");

        // Verify Stock restored to 10 and Customer debt reduced back to 0
        var prodAfterCancel = await _productService.GetByIdAsync("PROD-103");
        var custAfterCancel = await _customerService.GetByIdAsync("CUST-102");

        Assert.Equal(10.0, prodAfterCancel!.Stock);
        Assert.Equal(0.0, custAfterCancel!.Debt);
    }

    [Fact]
    public async Task ProcessPartialReturnAsync_ShouldCascadeKitRestockToIngredients()
    {
        // 1. Setup kit product and ingredients
        var ing1 = new ProductEntity { Id = "ING-01", Barcode = "111", Name = "Ingredient 1", Cost = 2.0, Price = 4.0, Stock = 10.0, Category = "Ing", Unit = "Pza" };
        var ing2 = new ProductEntity { Id = "ING-02", Barcode = "222", Name = "Ingredient 2", Cost = 3.0, Price = 6.0, Stock = 10.0, Category = "Ing", Unit = "Pza" };
        var kitProduct = new ProductEntity { Id = "KIT-01", Barcode = "333", Name = "Combo Desayuno", Cost = 5.0, Price = 15.0, Stock = 0.0, Category = "Combo", Unit = "Pza", IsKit = true };
        
        _context.Products.AddRange(ing1, ing2, kitProduct);

        var itemKitEntity = new ItemKitEntity
        {
            Id = "KIT-01-DEF",
            ParentProductId = "KIT-01",
            KitBarcode = "333",
            Name = "Combo Desayuno",
            Price = 15.0
        };
        itemKitEntity.Components.Add(new ItemKitItemEntity { Id = "COMP-1", ItemKitId = "KIT-01-DEF", ProductId = "ING-01", Quantity = 2.0 });
        itemKitEntity.Components.Add(new ItemKitItemEntity { Id = "COMP-2", ItemKitId = "KIT-01-DEF", ProductId = "ING-02", Quantity = 1.0 });
        
        _context.ItemKits.Add(itemKitEntity);
        await _context.SaveChangesAsync();

        // 2. Setup original sale
        var itemSnapshot = new SaleItemSnapshotDto("KIT-01", "Combo Desayuno", 15.0, 5.0, 1.0, "Pza", "Combo", 0.0, 15.0);
        var saleDto = new SaleDto(
            Id: "SALE-KIT-RETURN-TEST",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [itemSnapshot],
            Total: 15.0,
            TotalCost: 7.0,
            Profit: 8.0,
            PaidAmount: 15.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Efectivo",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );
        await _saleService.SaveAsync(saleDto);

        // Verify stock after sale but before return
        var ing1AfterSale = await _productService.GetByIdAsync("ING-01");
        var ing2AfterSale = await _productService.GetByIdAsync("ING-02");
        Assert.Equal(8.0, ing1AfterSale!.Stock);
        Assert.Equal(9.0, ing2AfterSale!.Stock);

        // 3. Process return of 1x Combo
        var success = await _saleService.ProcessPartialReturnAsync("SALE-KIT-RETURN-TEST", "KIT-01", 1.0, "Devolucion", "Efectivo");
        Assert.True(success);

        // 4. Verify parent SKU is still 0 stock, ingredient 1 has 10 stock, ingredient 2 has 10 stock
        var parent = await _productService.GetByIdAsync("KIT-01");
        var restoredIng1 = await _productService.GetByIdAsync("ING-01");
        var restoredIng2 = await _productService.GetByIdAsync("ING-02");

        Assert.Equal(0.0, parent!.Stock);
        Assert.Equal(10.0, restoredIng1!.Stock);
        Assert.Equal(10.0, restoredIng2!.Stock);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
