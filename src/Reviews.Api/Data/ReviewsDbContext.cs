using Microsoft.EntityFrameworkCore;
using Reviews.Api.Models;

namespace Reviews.Api.Data;

public class ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ProductIds match Catalog.Api's seeded products (see CatalogDbContext) for a consistent demo.
        modelBuilder.Entity<Review>().HasData(
            new Review
            {
                Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"),
                ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Keyboard
                Rating = 5,
                Comment = "Excellent switches, very responsive.",
                CreatedAtUtc = new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc)
            },
            new Review
            {
                Id = Guid.Parse("c1111111-2222-2222-2222-222222222222"),
                ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Keyboard
                Rating = 3,
                Comment = "Good but a bit loud.",
                CreatedAtUtc = new DateTime(2026, 1, 13, 14, 0, 0, DateTimeKind.Utc)
            },
            new Review
            {
                Id = Guid.Parse("c2222222-1111-1111-1111-111111111111"),
                ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Mouse
                Rating = 4,
                Comment = "Comfortable grip, tracks well.",
                CreatedAtUtc = new DateTime(2026, 1, 14, 11, 0, 0, DateTimeKind.Utc)
            });
    }
}
