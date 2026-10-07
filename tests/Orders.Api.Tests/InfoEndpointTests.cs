using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Orders.Api.Tests;

public class InfoEndpointTests(OrdersWebApplicationFactory factory) : IClassFixture<OrdersWebApplicationFactory>
{
    [Fact]
    public async Task Info_ReturnsServiceNameVersionAndPod()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("orders-api", body.GetProperty("service").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("pod").GetString()));
    }
}
