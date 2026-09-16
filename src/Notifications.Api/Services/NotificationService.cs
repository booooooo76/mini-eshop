using Microsoft.EntityFrameworkCore;
using Notifications.Api.Data;
using Notifications.Api.Models;
using Shared.Contracts;

namespace Notifications.Api.Services;

public class NotificationService(NotificationsDbContext db) : INotificationService
{
    public async Task<Notification> CreateFromOrderAsync(OrderCreatedEvent evt, CancellationToken ct = default)
    {
        var itemsCount = evt.Items.Sum(i => i.Quantity);
        var notification = new Notification
        {
            OrderId = evt.OrderId,
            Message = $"Order {evt.OrderId} confirmed ({itemsCount} item(s)). A confirmation email would be sent here."
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        return notification;
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken ct = default) =>
        await db.Notifications.AsNoTracking().OrderByDescending(n => n.SentAtUtc).ToListAsync(ct);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
}
