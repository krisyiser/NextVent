using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Implementations;
using Xunit;

namespace Ticketfy.Desktop.Tests;

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

    [Fact]
    public async Task SaveAsync_ShouldProrateGlobalDiscountAndCalculateVAT()
    {
        var prodA = new ProductDto("PROD-A", "1001", "Item A", 50.0, 100.0, 0, 0, 10, "General", "Pza", 0, null);
        var prodB = new ProductDto("PROD-B", "1002", "Item B", 25.0, 50.0, 0, 0, 10, "General", "Pza", 0, null);
        await _productService.AddAsync(prodA);
        await _productService.AddAsync(prodB);

        var itemA = new SaleItemSnapshotDto("PROD-A", "Item A", 100.0, 50.0, 1, "Pza", "General", 0.0, 100.0);
        var itemB = new SaleItemSnapshotDto("PROD-B", "Item B", 50.0, 25.0, 1, "Pza", "General", 0.0, 50.0);

        var saleDto = new SaleDto(
            Id: "SALE-PRORATE-TEST",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [itemA, itemB],
            Total: 120.0,
            TotalCost: 75.0,
            Profit: 45.0,
            PaidAmount: 120.0,
            ChangeAmount: 0.0,
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
        Assert.NotNull(savedSale);

        var savedItems = savedSale.Items;
        Assert.Equal(2, savedItems.Count);

        var savedA = savedItems.First(i => i.ProductId == "PROD-A");
        var savedB = savedItems.First(i => i.ProductId == "PROD-B");

        Assert.Equal(20.0, savedA.ProratedGlobalDiscountAmount);
        Assert.Equal(12.80, savedA.TaxAmount);
        Assert.Equal(92.80, savedA.TotalPrice);

        Assert.Equal(10.0, savedB.ProratedGlobalDiscountAmount);
        Assert.Equal(6.40, savedB.TaxAmount);
        Assert.Equal(46.40, savedB.TotalPrice);
    }

    [Fact]
    public async Task ProcessPartialReturnAsync_ShouldEnforceAntiFraudQuantityLimit()
    {
        var product = new ProductDto("PROD-X", "9999", "Item X", 10.0, 20.0, 0, 0, 10, "General", "Pza", 0, null);
        await _productService.AddAsync(product);

        var item = new SaleItemSnapshotDto("PROD-X", "Item X", 20.0, 10.0, 2, "Pza", "General", 0.0, 40.0);
        var saleDto = new SaleDto(
            Id: "SALE-ANTI-FRAUD",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [item],
            Total: 40.0,
            TotalCost: 20.0,
            Profit: 20.0,
            PaidAmount: 40.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Cash",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        await _saleService.SaveAsync(saleDto);

        var success = await _saleService.ProcessPartialReturnAsync("SALE-ANTI-FRAUD", "PROD-X", 1.0, "Reason");
        Assert.True(success);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _saleService.ProcessPartialReturnAsync("SALE-ANTI-FRAUD", "PROD-X", 2.0, "Reason");
        });
    }

    [Fact]
    public async Task RegisterPurchaseAsync_ShouldBlockNonPositiveCosts()
    {
        var purchaseService = new PurchaseService(_context);

        var items = new List<PurchaseItemDto>
        {
            new PurchaseItemDto("ITEM-1", "PURCHASE-1", "PROD-1", "Product 1", 0.0, 10, 0.0)
        };

        var purchaseDto = new PurchaseDto(
            Id: "PURCHASE-1",
            SupplierId: "SUPP-1",
            SupplierName: "Supplier 1",
            InvoiceNumber: "INV-123",
            Date: DateTime.Now.ToString("g"),
            TotalCost: 0.0,
            Notes: "Test notes",
            Items: items
        );

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await purchaseService.RegisterPurchaseAsync(purchaseDto);
        });
    }

    [Fact]
    public void TestDtoSerialization()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        var item = new SaleItemSnapshotDto(
            ProductId: "PROD-A",
            Name: "Item A",
            UnitPrice: 100.0,
            Cost: 50.0,
            Quantity: 1.0,
            Unit: "Pza",
            Category: "General",
            Discount: 0.0,
            TotalPrice: 92.8,
            OriginalUnitPrice: 100.0,
            AppliedDiscountAmount: 0.0,
            AppliedPromotionId: null,
            ProratedGlobalDiscountAmount: 20.0,
            TaxAmount: 12.8,
            ReturnedQuantity: 0.0
        );

        var json = System.Text.Json.JsonSerializer.Serialize(item, options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SaleItemSnapshotDto>(json, options);

        Assert.Equal(20.0, deserialized.ProratedGlobalDiscountAmount);
        Assert.Equal(12.8, deserialized.TaxAmount);
    }

    [Fact]
    public async Task SaveAsync_ShouldDistributeLostPennyDiscountRemainderToLastItem()
    {
        var p1 = new ProductDto("P1", "1", "Item 1", 5.0, 10.0, 0, 0, 10, "General", "Pza", 0, null);
        var p2 = new ProductDto("P2", "2", "Item 2", 5.0, 10.0, 0, 0, 10, "General", "Pza", 0, null);
        var p3 = new ProductDto("P3", "3", "Item 3", 5.0, 10.0, 0, 0, 10, "General", "Pza", 0, null);
        await _productService.AddAsync(p1);
        await _productService.AddAsync(p2);
        await _productService.AddAsync(p3);

        var item1 = new SaleItemSnapshotDto("P1", "Item 1", 10.0, 5.0, 1, "Pza", "General", 0.0, 10.0);
        var item2 = new SaleItemSnapshotDto("P2", "Item 2", 10.0, 5.0, 1, "Pza", "General", 0.0, 10.0);
        var item3 = new SaleItemSnapshotDto("P3", "Item 3", 10.0, 5.0, 1, "Pza", "General", 0.0, 10.0);

        var saleDto = new SaleDto(
            Id: "SALE-LOST-PENNY",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [item1, item2, item3],
            Total: 20.0,
            TotalCost: 15.0,
            Profit: 5.0,
            PaidAmount: 20.0,
            ChangeAmount: 0.0,
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
        var items = savedSale.Items;
        Assert.Equal(3, items.Count);

        var saved1 = items.First(i => i.ProductId == "P1");
        var saved2 = items.First(i => i.ProductId == "P2");
        var saved3 = items.First(i => i.ProductId == "P3");

        Assert.Equal(3.33, saved1.ProratedGlobalDiscountAmount);
        Assert.Equal(3.33, saved2.ProratedGlobalDiscountAmount);
        Assert.Equal(3.34, saved3.ProratedGlobalDiscountAmount);

        double sumDiscounts = items.Sum(i => i.ProratedGlobalDiscountAmount);
        Assert.Equal(10.00, sumDiscounts);
    }

    [Fact]
    public async Task ProcessSaleAsync_ShouldDeductNestedKitStockRecursively()
    {
        var apple = new ProductDto("APPLE", "111", "Apple", 2.0, 5.0, 0, 0, 10, "Fruits", "Pza", 0, null);
        await _productService.AddAsync(apple);

        var kitAProd = new ProductEntity { Id = "KITA", Barcode = "222", Name = "Kit A", Cost = 0.0, Price = 0.0, Stock = 0, Category = "Combo", Unit = "Pza", IsKit = true };
        _context.Products.Add(kitAProd);

        var kitA = new ItemKitEntity { Id = "KITA-DEF", ParentProductId = "KITA", KitBarcode = "222", Name = "Kit A", Price = 5.0 };
        kitA.Components.Add(new ItemKitItemEntity { Id = "COMP-A1", ItemKitId = "KITA-DEF", ProductId = "APPLE", Quantity = 1.0 });
        _context.ItemKits.Add(kitA);

        var kitBProd = new ProductEntity { Id = "KITB", Barcode = "333", Name = "Kit B", Cost = 0.0, Price = 0.0, Stock = 0, Category = "Combo", Unit = "Pza", IsKit = true };
        _context.Products.Add(kitBProd);

        var kitB = new ItemKitEntity { Id = "KITB-DEF", ParentProductId = "KITB", KitBarcode = "333", Name = "Kit B", Price = 6.0 };
        kitB.Components.Add(new ItemKitItemEntity { Id = "COMP-B1", ItemKitId = "KITB-DEF", ProductId = "KITA", Quantity = 1.0 });
        _context.ItemKits.Add(kitB);

        await _context.SaveChangesAsync();

        var item = new SaleItemSnapshotDto("KITB", "Kit B", 6.0, 2.0, 1.0, "Pza", "Combo", 0.0, 6.0);
        var saleDto = new SaleDto(
            Id: "SALE-NESTED-KIT",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [item],
            Total: 6.0,
            TotalCost: 2.0,
            Profit: 4.0,
            PaidAmount: 6.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Cash",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        await _saleService.SaveAsync(saleDto);

        var appleDb = await _context.Products.FindAsync("APPLE");
        Assert.Equal(9.0, appleDb.Stock);
    }

    [Fact]
    public async Task ProcessSaleAsync_ShouldThrowOnCircularKitDependency()
    {
        var kitAProd = new ProductEntity { Id = "KITA-CIRC", Barcode = "444", Name = "Kit A Circ", Cost = 0.0, Price = 0.0, Stock = 0, Category = "Combo", Unit = "Pza", IsKit = true };
        _context.Products.Add(kitAProd);

        var kitA = new ItemKitEntity { Id = "KITACIRC-DEF", ParentProductId = "KITA-CIRC", KitBarcode = "444", Name = "Kit A Circ", Price = 5.0 };
        kitA.Components.Add(new ItemKitItemEntity { Id = "COMP-C1", ItemKitId = "KITACIRC-DEF", ProductId = "KITA-CIRC", Quantity = 1.0 });
        _context.ItemKits.Add(kitA);
        await _context.SaveChangesAsync();

        var item = new SaleItemSnapshotDto("KITA-CIRC", "Kit A Circ", 5.0, 2.0, 1.0, "Pza", "Combo", 0.0, 5.0);
        var saleDto = new SaleDto(
            Id: "SALE-CIRC-KIT",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [item],
            Total: 5.0,
            TotalCost: 2.0,
            Profit: 3.0,
            PaidAmount: 5.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Cash",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _saleService.SaveAsync(saleDto);
        });
    }

    [Fact]
    public async Task ProcessPartialReturnAsync_ShouldNotRestockAndLogShrinkage_WhenProductIsNotInGoodCondition()
    {
        // 1. Setup Product
        var product = new ProductDto("SHRINK-PROD", "777", "Damaged Item", 4.0, 10.0, 8.0, 5, 10, "Botanas", "Pza", 0, null);
        await _productService.AddAsync(product);

        // 2. Perform Sale
        var item = new SaleItemSnapshotDto("SHRINK-PROD", "Damaged Item", 10.0, 4.0, 2, "Pza", "Botanas", 0.0, 20.0);
        var saleDto = new SaleDto(
            Id: "SALE-SHRINK-01",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [item],
            Total: 20.0,
            TotalCost: 8.0,
            Profit: 12.0,
            PaidAmount: 20.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Cash",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );
        await _saleService.SaveAsync(saleDto);

        // Check stock after sale (10 - 2 = 8)
        var prodAfterSale = await _productService.GetByIdAsync("SHRINK-PROD");
        Assert.Equal(8.0, prodAfterSale.Stock);

        // 3. Process return with isProductInGoodCondition = false
        var result = await _saleService.ProcessPartialReturnAsync(
            "SALE-SHRINK-01",
            "SHRINK-PROD",
            1.0,
            "Rotura",
            "Efectivo",
            isProductInGoodCondition: false
        );

        Assert.True(result);

        // 4. Verify Stock was NOT incremented (remains 8.0)
        var prodAfterReturn = await _productService.GetByIdAsync("SHRINK-PROD");
        Assert.Equal(8.0, prodAfterReturn.Stock);

        // 5. Verify AuditLogEntity created for the shrinkage
        var logs = await _context.AuditLogs.ToListAsync();
        var shrinkageLog = logs.FirstOrDefault(l => l.EntityId == "SHRINK-PROD" && l.Reason.Contains("Devolución de producto dañado/mermado"));
        Assert.NotNull(shrinkageLog);
        Assert.Equal(4.0, shrinkageLog.FinancialImpact); // Cost = 4.0 * Quantity = 1.0 -> 4.0
    }

    [Fact]
    public async Task CloseAsync_ShouldCreateShiftMovementForShortage_WhenActualBalanceIsLowerThanExpected()
    {
        var shiftService = new ShiftService(_context);

        // 1. Open shift
        var activeShift = await shiftService.OpenAsync(100.0); // opening balance = 100

        // 2. Perform cash sale
        var product = new ProductDto("CASH-CUT-PROD", "888", "Ice Cream", 5.0, 10.0, 8.0, 5, 20, "Botanas", "Pza", 0, null);
        await _productService.AddAsync(product);

        var item = new SaleItemSnapshotDto("CASH-CUT-PROD", "Ice Cream", 10.0, 5.0, 10, "Pza", "Botanas", 0.0, 100.0);
        var saleDto = new SaleDto(
            Id: "SALE-CASH-CUT-01",
            Date: DateTimeOffset.UtcNow.ToString("o"),
            Items: [item],
            Total: 100.0,
            TotalCost: 50.0,
            Profit: 50.0,
            PaidAmount: 100.0,
            ChangeAmount: 0.0,
            PaymentMethod: "Cash",
            CustomerId: null,
            IsCredit: false,
            IsCancelled: false,
            CancelledAt: null,
            EstadoFiscal: "PENDIENTE",
            UuidSat: null,
            SerieFolio: null
        );
        await _saleService.SaveAsync(saleDto);

        // Expected balance: opening (100) + sale (100) = 200
        // 3. Close shift declaring only 180 (shortage of 20)
        var closedShift = await shiftService.CloseAsync(activeShift.Id, 180.0);

        Assert.Equal(200.0, closedShift.ExpectedBalance);
        Assert.Equal(180.0, closedShift.ActualBalance);
        Assert.Equal(-20.0, closedShift.Diff);

        // 4. Verify ShiftMovement was added for the shortage
        var movements = await _context.ShiftMovements.ToListAsync();
        var shortageMovement = movements.FirstOrDefault(m => m.ShiftId == activeShift.Id && m.Description.Contains("Faltante de Caja"));
        Assert.NotNull(shortageMovement);
        Assert.Equal(20.0, shortageMovement.Amount);
        Assert.True(shortageMovement.IsOutflow);
    }

    [Fact]
    public async Task RechargeAsync_ShouldIncreaseGiftcardBalanceAndRecordShiftMovement()
    {
        var giftcardService = new GiftcardService(_context);

        // 1. Create card
        await giftcardService.CreateCardAsync("GIFT-999", 50.0, null);
        var card = await giftcardService.GetByCardNumberAsync("GIFT-999");
        Assert.NotNull(card);

        // Add active shift to prevent foreign key failure
        var shift = new ShiftEntity
        {
            Id = "SHIFT-TEST-01",
            OpeningBalance = 100.0,
            IsOpen = 1,
            StartTime = DateTimeOffset.UtcNow.ToString("o")
        };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        // 2. Recharge card with 150.0 cash under active shift
        await giftcardService.RechargeAsync(card.Id, 150.0m, Ticketfy.Core.Enums.PaymentMethod.Efectivo, "SHIFT-TEST-01");

        // 3. Verify balance (50 + 150 = 200)
        var updatedCard = await giftcardService.GetByCardNumberAsync("GIFT-999");
        Assert.NotNull(updatedCard);
        Assert.Equal(200.0, updatedCard.Balance);

        // 4. Verify ShiftMovement created for the recharge inflow
        var movements = await _context.ShiftMovements.ToListAsync();
        var rechargeMovement = movements.FirstOrDefault(m => m.ShiftId == "SHIFT-TEST-01" && m.Description.Contains("Recarga Monedero"));
        Assert.NotNull(rechargeMovement);
        Assert.Equal(150.0, rechargeMovement.Amount);
        Assert.False(rechargeMovement.IsOutflow);
    }

    [Fact]
    public void EscPosPrinterService_ShouldEncodeSpanishCharactersToCp858Bytes()
    {
        // 1. Register encoding provider
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 2. Fetch CP858 encoding
        var printerEncoding = System.Text.Encoding.GetEncoding(858);
        Assert.NotNull(printerEncoding);

        // 3. Test conversion of "ñáéíóú"
        var text = "ñáéíóú";
        var bytes = printerEncoding.GetBytes(text);

        Assert.Equal(6, bytes.Length);
        // Under CP858: ñ is 0xA4, á is 0xA0, é is 0x82, í is 0xA1, ó is 0xA2, ú is 0xA3
        Assert.Equal(0xA4, bytes[0]);
        Assert.Equal(0xA0, bytes[1]);
        Assert.Equal(0x82, bytes[2]);
        Assert.Equal(0xA1, bytes[3]);
        Assert.Equal(0xA2, bytes[4]);
        Assert.Equal(0xA3, bytes[5]);
    }

    [Fact]
    public async Task BackupService_ShouldCreateBackupSuccessfully()
    {
        // 1. Create a dummy database file in LocalAppData
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(appDataFolder, "Ticketfy", "Database");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        var dbPath = Path.Combine(appFolder, "ticketfy.db");
        bool createdDummy = false;
        if (!File.Exists(dbPath))
        {
            await File.WriteAllTextAsync(dbPath, "SQLite format 3\0 dummy db data");
            createdDummy = true;
        }

        var backupService = new Ticketfy.Services.Implementations.BackupService();
        var result = await backupService.CreateZCutBackupAsync("SHIFT-TEST");

        Assert.True(result);

        // 2. Verify that the backup file exists in the Backups folder
        var backupDir = Path.Combine(appDataFolder, "Ticketfy", "Backups");
        Assert.True(Directory.Exists(backupDir));

        var files = Directory.GetFiles(backupDir, "backup_ZCut_SHIFT-TEST_*.db");
        Assert.NotEmpty(files);

        // Clean up
        foreach (var file in files)
        {
            File.Delete(file);
        }

        if (createdDummy && File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task EscPosPrinterService_ShouldSupportGracefulDisposeAsync()
    {
        var printerService = new Ticketfy.Services.Implementations.EscPosPrinterService();
        
        // Simply dispose and check no exceptions are thrown
        await printerService.DisposeAsync();
    }

    [Fact]
    public void DateTimeExtensions_ShouldConvertBetweenUtcAndMexicoCityTimezone()
    {
        // 2026-08-10 05:30:00 UTC = 2026-08-09 23:30:00 America/Mexico_City
        var utcDate = new DateTime(2026, 8, 10, 5, 30, 0, DateTimeKind.Utc);
        var localDate = Ticketfy.Core.Helpers.DateTimeExtensions.ToBusinessLocalTime(utcDate);

        Assert.Equal(2026, localDate.Year);
        Assert.Equal(8, localDate.Month);
        Assert.Equal(9, localDate.Day);
        Assert.Equal(23, localDate.Hour);
        Assert.Equal(30, localDate.Minute);

        var utcBack = Ticketfy.Core.Helpers.DateTimeExtensions.ToBusinessUtcTime(localDate);
        Assert.Equal(utcDate, utcBack);
    }

    [Fact]
    public async Task PosViewModel_ShouldAutoSaveAndRehydrateCartItems()
    {
        var posVm = new Ticketfy.ViewModels.PosViewModel(_productService);
        await Task.Delay(200);

        var item = new CartItemDto("PROD-TEST", "Refresco", 15.0, 2.0, "Pza");

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            posVm.CartItems.Add(item);
            await Task.Delay(200);

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string draftCartPath = Path.Combine(appDataFolder, "Ticketfy", "DraftCart.json");
            Assert.True(File.Exists(draftCartPath));

            var posVm2 = new Ticketfy.ViewModels.PosViewModel(_productService);
            await Task.Delay(200);

            Assert.Single(posVm2.CartItems);
            Assert.Equal("PROD-TEST", posVm2.CartItems[0].Id);
            Assert.Equal(2.0, posVm2.CartItems[0].Quantity);

            posVm2.CartItems.Clear();
            await Task.Delay(200);
            Assert.False(File.Exists(draftCartPath));
        });
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
