using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Catalog.Api.Tests;

public class InfoEndpointTests(CatalogWebApplicationFactory factory) : IClassFixture<CatalogWebApplicationFactory>
{
    [Fact]
    public async Task Info_ReturnsServiceNameVersionAndPod()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("catalog-api", body.GetProperty("service").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("pod").GetString()));
    }
}
