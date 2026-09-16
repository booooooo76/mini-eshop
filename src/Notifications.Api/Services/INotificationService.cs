using Notifications.Api.Models;
using Shared.Contracts;

namespace Notifications.Api.Services;

public interface INotificationService
{
    Task<Notification> CreateFromOrderAsync(OrderCreatedEvent evt, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
