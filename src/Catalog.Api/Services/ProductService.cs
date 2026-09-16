using System.Text.Json;
using Catalog.Api.Data;
using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Api.Services;

public class ProductService(CatalogDbContext db, IDistributedCache cache) : IProductService
{
    private const string AllProductsCacheKey = "products:all";

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetStringAsync(AllProductsCacheKey, ct);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<Product>>(cached) ?? [];
        }

        var products = await db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

        await cache.SetStringAsync(
            AllProductsCacheKey,
            JsonSerializer.Serialize(products),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) },
            ct);

        return products;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        ValidateFields(product.Name, product.Price, product.Stock);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(AllProductsCacheKey, ct);
        return product;
    }

    public async Task<Product?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        ValidateFields(request.Name, request.Price, request.Stock);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(AllProductsCacheKey, ct);
        return product;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return false;
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(AllProductsCacheKey, ct);
        return true;
    }

    private static void ValidateFields(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative.");
        }

        if (stock < 0)
        {
            throw new ArgumentException("Stock cannot be negative.");
        }
    }

    public async Task DecreaseStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null)
        {
            return;
        }

        product.Stock = Math.Max(0, product.Stock - quantity);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(AllProductsCacheKey, ct);
    }
}
