using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 50 },
            new Product { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 100 },
            new Product { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Monitor", Description = "27-inch 4K monitor", Price = 349.99m, Stock = 20 });
    }
}
