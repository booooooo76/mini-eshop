namespace Orders.Api.Models;

public record CreateOrderRequest(List<CreateOrderItem> Items);

public record CreateOrderItem(Guid ProductId, int Quantity);
