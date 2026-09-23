using System.Net;
using System.Net.Http.Json;
using Orders.Api.Models;

namespace Orders.Api.Tests;

public class OrdersControllerTests(OrdersWebApplicationFactory factory) : IClassFixture<OrdersWebApplicationFactory>
{
    [Fact]
    public async Task Create_ThenGetById_ReturnsOrder()
    {
        var client = factory.CreateClient();
        var request = new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 3)]);

        var createResponse = await client.PostAsJsonAsync("/api/orders", request);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();

        var getResponse = await client.GetAsync($"/api/orders/{created!.Id}");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_WithZeroQuantity_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var request = new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 0)]);

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNoItems_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var request = new CreateOrderRequest([]);

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ThenGetById_ReturnsUpdatedOrder()
    {
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();

        var updateResponse = await client.PutAsJsonAsync($"/api/orders/{created!.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<Order>();
        Assert.Equal(OrderStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/orders/{Guid.NewGuid()}/status", new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest([new CreateOrderItem(Guid.NewGuid(), 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();

        var deleteResponse = await client.DeleteAsync($"/api/orders/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
