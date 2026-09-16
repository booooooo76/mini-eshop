using System.Net;
using Xunit;

namespace Notifications.Api.Tests;

public class NotificationsControllerTests(NotificationsWebApplicationFactory factory) : IClassFixture<NotificationsWebApplicationFactory>
{
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notifications");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/notifications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PatchAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
