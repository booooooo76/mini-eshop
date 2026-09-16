using System.Net;
using System.Net.Http.Json;
using Orders.Api.Models;
using Xunit;

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
}
