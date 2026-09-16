using Microsoft.EntityFrameworkCore;
using Notifications.Api.Models;

namespace Notifications.Api.Data;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // OrderIds match Orders.Api's seeded orders (see OrdersDbContext) for a consistent demo.
        modelBuilder.Entity<Notification>().HasData(
            new Notification
            {
                Id = Guid.Parse("b1111111-1111-1111-1111-111111111111"),
                OrderId = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                Message = "Order a1111111-1111-1111-1111-111111111111 confirmed (1 item(s)). A confirmation email would be sent here.",
                SentAtUtc = new DateTime(2026, 1, 10, 10, 0, 5, DateTimeKind.Utc),
                IsRead = true
            },
            new Notification
            {
                Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
                OrderId = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                Message = "Order a2222222-2222-2222-2222-222222222222 confirmed (2 item(s)). A confirmation email would be sent here.",
                SentAtUtc = new DateTime(2026, 1, 11, 15, 30, 5, DateTimeKind.Utc),
                IsRead = false
            });
    }
}
