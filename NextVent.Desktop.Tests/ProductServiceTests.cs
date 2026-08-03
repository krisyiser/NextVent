using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Services.Implementations;
using Xunit;

namespace NextVent.Desktop.Tests;

public sealed class ProductServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _service = new ProductService(_context);
    }

    [Fact]
    public async Task AddAndGetByBarcode_ShouldReturnCorrectProduct()
    {
        var dto = new ProductDto(
            Id: "PROD-TEST-001",
            Barcode: "7501112223334",
            Name: "Test Beverage 500ml",
            Cost: 10.0,
            Price: 15.0,
            WholesalePrice: 12.0,
            WholesaleThreshold: 10,
            Stock: 50,
            Category: "Bebidas",
            Unit: "Pza",
            ExpiresSoon: 0,
            CreatedAt: DateTimeOffset.UtcNow.ToString("o")
        );

        await _service.AddAsync(dto);

        var retrieved = await _service.GetByBarcodeAsync("7501112223334");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Beverage 500ml", retrieved.Name);
        Assert.Equal(15.0, retrieved.Price);
        Assert.Equal(50, retrieved.Stock);
    }

    [Fact]
    public async Task UpdateProduct_ShouldModifyExistingRecord()
    {
        var dto = new ProductDto("PROD-TEST-002", "7509998887776", "Original Name", 5.0, 10.0, 8.0, 5, 20, "Abarrotes", "Pza", 0, null);
        await _service.AddAsync(dto);

        var updatedDto = dto with { Name = "Updated Name", Price = 12.5, Stock = 30 };
        await _service.UpdateAsync(updatedDto);

        var retrieved = await _service.GetByIdAsync("PROD-TEST-002");
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
        Assert.Equal(12.5, retrieved.Price);
        Assert.Equal(30, retrieved.Stock);
    }

    [Fact]
    public async Task DeleteProduct_ShouldRemoveFromDatabase()
    {
        var dto = new ProductDto("PROD-TEST-003", "7500000000001", "To Delete", 5.0, 10.0, 0, 0, 10, "Abarrotes", "Pza", 0, null);
        await _service.AddAsync(dto);

        await _service.DeleteAsync("PROD-TEST-003");

        var retrieved = await _service.GetByIdAsync("PROD-TEST-003");
        Assert.Null(retrieved);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
