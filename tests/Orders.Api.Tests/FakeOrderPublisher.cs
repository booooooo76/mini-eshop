using Orders.Api.Messaging;
using Shared.Contracts;

namespace Orders.Api.Tests;

public class FakeOrderPublisher : IOrderPublisher
{
    public List<OrderCreatedEvent> Published { get; } = [];

    public Task PublishAsync(OrderCreatedEvent evt, CancellationToken ct = default)
    {
        Published.Add(evt);
        return Task.CompletedTask;
    }
}
