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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WithNonPositiveQuantity_Throws(int quantity)
    {
        var db = CreateDb($"{nameof(CreateAsync_WithNonPositiveQuantity_Throws)}_{quantity}");
        var service = new OrderService(db, new FakeOrderPublisher());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), quantity)])));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyProductId_Throws()
    {
        var db = CreateDb(nameof(CreateAsync_WithEmptyProductId_Throws));
        var service = new OrderService(db, new FakeOrderPublisher());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateOrderRequest([new CreateOrderItem(Guid.Empty, 1)])));
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatus()
    {
        var db = CreateDb(nameof(UpdateStatusAsync_ChangesStatus));
        var service = new OrderService(db, new FakeOrderPublisher());
        var order = await service.CreateAsync(new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 1)]));

        var updated = await service.UpdateStatusAsync(order.Id, OrderStatus.Cancelled);

        Assert.NotNull(updated);
        Assert.Equal(OrderStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownId_ReturnsNull()
    {
        var db = CreateDb(nameof(UpdateStatusAsync_UnknownId_ReturnsNull));
        var service = new OrderService(db, new FakeOrderPublisher());

        var updated = await service.UpdateStatusAsync(Guid.NewGuid(), OrderStatus.Cancelled);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOrder()
    {
        var db = CreateDb(nameof(DeleteAsync_RemovesOrder));
        var service = new OrderService(db, new FakeOrderPublisher());
        var order = await service.CreateAsync(new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 1)]));

        var deleted = await service.DeleteAsync(order.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(order.Id));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var db = CreateDb(nameof(DeleteAsync_UnknownId_ReturnsFalse));
        var service = new OrderService(db, new FakeOrderPublisher());

        var deleted = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
