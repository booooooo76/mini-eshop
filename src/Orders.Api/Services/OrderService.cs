using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Messaging;
using Orders.Api.Models;
using Shared.Contracts;

namespace Orders.Api.Services;

public class OrderService(OrdersDbContext db, IOrderPublisher publisher) : IOrderService
{
    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.");
        }

        var order = new Order
        {
            Items = request.Items.Select(i => new OrderItem { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        await publisher.PublishAsync(
            new OrderCreatedEvent(
                order.Id,
                order.CreatedAtUtc,
                order.Items.Select(i => new OrderCreatedItem(i.ProductId, i.Quantity)).ToList()),
            ct);

        return order;
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) =>
        await db.Orders.Include(o => o.Items).AsNoTracking().ToListAsync(ct);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Orders.Include(o => o.Items).AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
}
