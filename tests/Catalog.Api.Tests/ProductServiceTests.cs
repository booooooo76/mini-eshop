using Catalog.Api.Data;
using Catalog.Api.Models;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.Api.Tests;

public class ProductServiceTests
{
    private static (CatalogDbContext Db, IDistributedCache Cache) CreateDependencies(string dbName)
    {
        var dbOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var cache = services.BuildServiceProvider().GetRequiredService<IDistributedCache>();

        return (new CatalogDbContext(dbOptions), cache);
    }

    [Fact]
    public async Task CreateAsync_PersistsProduct()
    {
        var (db, cache) = CreateDependencies(nameof(CreateAsync_PersistsProduct));
        var service = new ProductService(db, cache);

        var product = await service.CreateAsync(new Product { Name = "Headset", Price = 49.99m, Stock = 10 });

        var stored = await service.GetByIdAsync(product.Id);
        Assert.NotNull(stored);
        Assert.Equal("Headset", stored!.Name);
    }

    [Fact]
    public async Task CreateAsync_WithNegativePrice_Throws()
    {
        var (db, cache) = CreateDependencies(nameof(CreateAsync_WithNegativePrice_Throws));
        var service = new ProductService(db, cache);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new Product { Name = "Bad", Price = -1m, Stock = 1 }));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_Throws()
    {
        var (db, cache) = CreateDependencies(nameof(CreateAsync_WithEmptyName_Throws));
        var service = new ProductService(db, cache);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new Product { Name = "  ", Price = 1m, Stock = 1 }));
    }

    [Fact]
    public async Task DecreaseStockAsync_NeverGoesBelowZero()
    {
        var (db, cache) = CreateDependencies(nameof(DecreaseStockAsync_NeverGoesBelowZero));
        var service = new ProductService(db, cache);
        var product = await service.CreateAsync(new Product { Name = "Cable", Price = 4.99m, Stock = 3 });

        await service.DecreaseStockAsync(product.Id, 10);

        var stored = await service.GetByIdAsync(product.Id);
        Assert.Equal(0, stored!.Stock);
    }

    [Fact]
    public async Task GetAllAsync_ServesStaleDataFromCache()
    {
        var (db, cache) = CreateDependencies(nameof(GetAllAsync_ServesStaleDataFromCache));
        var service = new ProductService(db, cache);
        await service.CreateAsync(new Product { Name = "Webcam", Price = 59.99m, Stock = 5 });

        var first = await service.GetAllAsync();
        db.Products.RemoveRange(db.Products);
        await db.SaveChangesAsync();

        var second = await service.GetAllAsync();

        Assert.Single(first);
        Assert.Single(second);
    }
}
