using Microsoft.EntityFrameworkCore;
using Orders.Api.Models;

namespace Orders.Api.Data;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId);

        // Ids match Catalog.Api's seeded products (see CatalogDbContext) so the two
        // services show consistent demo data out of the box.
        var order1Id = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        var order2Id = Guid.Parse("a2222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = order1Id,
                CreatedAtUtc = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Confirmed
            },
            new Order
            {
                Id = order2Id,
                CreatedAtUtc = new DateTime(2026, 1, 11, 15, 30, 0, DateTimeKind.Utc),
                Status = OrderStatus.Cancelled
            });

        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem
            {
                Id = Guid.Parse("a1111111-0001-0001-0001-000000000001"),
                OrderId = order1Id,
                ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Keyboard
                Quantity = 1
            },
            new OrderItem
            {
                Id = Guid.Parse("a2222222-0001-0001-0001-000000000001"),
                OrderId = order2Id,
                ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Mouse
                Quantity = 2
            });
    }
}
