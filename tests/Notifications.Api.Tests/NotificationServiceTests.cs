using Microsoft.EntityFrameworkCore;
using Notifications.Api.Data;
using Notifications.Api.Services;
using Shared.Contracts;
using Xunit;

namespace Notifications.Api.Tests;

public class NotificationServiceTests
{
    private static NotificationsDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new NotificationsDbContext(options);
    }

    [Fact]
    public async Task CreateFromOrderAsync_PersistsNotification()
    {
        var db = CreateDb(nameof(CreateFromOrderAsync_PersistsNotification));
        var service = new NotificationService(db);
        var evt = new OrderCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, [new OrderCreatedItem(Guid.NewGuid(), 3)]);

        var notification = await service.CreateFromOrderAsync(evt);

        var stored = await service.GetByIdAsync(notification.Id);
        Assert.NotNull(stored);
        Assert.Equal(evt.OrderId, stored!.OrderId);
        Assert.Contains(evt.OrderId.ToString(), stored.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestFirst()
    {
        var db = CreateDb(nameof(GetAllAsync_ReturnsNewestFirst));
        var service = new NotificationService(db);
        var first = await service.CreateFromOrderAsync(new OrderCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, [new OrderCreatedItem(Guid.NewGuid(), 1)]));
        await Task.Delay(10);
        var second = await service.CreateFromOrderAsync(new OrderCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, [new OrderCreatedItem(Guid.NewGuid(), 1)]));

        var all = await service.GetAllAsync();

        Assert.Equal(second.Id, all[0].Id);
        Assert.Equal(first.Id, all[1].Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsIsRead()
    {
        var db = CreateDb(nameof(MarkAsReadAsync_SetsIsRead));
        var service = new NotificationService(db);
        var notification = await service.CreateFromOrderAsync(new OrderCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, [new OrderCreatedItem(Guid.NewGuid(), 1)]));

        var marked = await service.MarkAsReadAsync(notification.Id);

        Assert.True(marked);
        var stored = await service.GetByIdAsync(notification.Id);
        Assert.True(stored!.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_UnknownId_ReturnsFalse()
    {
        var db = CreateDb(nameof(MarkAsReadAsync_UnknownId_ReturnsFalse));
        var service = new NotificationService(db);

        var marked = await service.MarkAsReadAsync(Guid.NewGuid());

        Assert.False(marked);
    }

    [Fact]
    public async Task DeleteAsync_RemovesNotification()
    {
        var db = CreateDb(nameof(DeleteAsync_RemovesNotification));
        var service = new NotificationService(db);
        var notification = await service.CreateFromOrderAsync(new OrderCreatedEvent(Guid.NewGuid(), DateTime.UtcNow, [new OrderCreatedItem(Guid.NewGuid(), 1)]));

        var deleted = await service.DeleteAsync(notification.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(notification.Id));
    }
}
