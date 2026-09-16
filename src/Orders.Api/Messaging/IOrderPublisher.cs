using Shared.Contracts;

namespace Orders.Api.Messaging;

public interface IOrderPublisher
{
    Task PublishAsync(OrderCreatedEvent evt, CancellationToken ct = default);
}
