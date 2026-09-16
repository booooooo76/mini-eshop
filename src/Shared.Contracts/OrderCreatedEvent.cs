namespace Shared.Contracts;

public record OrderCreatedEvent(
    Guid OrderId,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderCreatedItem> Items);

public record OrderCreatedItem(Guid ProductId, int Quantity);
