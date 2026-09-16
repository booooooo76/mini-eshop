using Orders.Api.Models;

namespace Orders.Api.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
