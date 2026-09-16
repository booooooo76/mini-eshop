using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Models;
using Orders.Api.Services;
using Xunit;

namespace Orders.Api.Tests;

public class OrderServiceTests
{
    private static OrdersDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new OrdersDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsOrder_AndPublishesEvent()
    {
        var db = CreateDb(nameof(CreateAsync_PersistsOrder_AndPublishesEvent));
        var publisher = new FakeOrderPublisher();
        var service = new OrderService(db, publisher);

        var order = await service.CreateAsync(new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 2)]));

        var stored = await service.GetByIdAsync(order.Id);
        Assert.NotNull(stored);
        Assert.Single(publisher.Published);
        Assert.Equal(order.Id, publisher.Published[0].OrderId);
    }

    [Fact]
    public async Task CreateAsync_WithNoItems_Throws()
    {
        var db = CreateDb(nameof(CreateAsync_WithNoItems_Throws));
        var service = new OrderService(db, new FakeOrderPublisher());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new CreateOrderRequest([])));
    }
}
