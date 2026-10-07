using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Reviews.Api.Tests;

public class InfoEndpointTests(ReviewsWebApplicationFactory factory) : IClassFixture<ReviewsWebApplicationFactory>
{
    [Fact]
    public async Task Info_ReturnsServiceNameVersionAndPod()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("reviews-api", body.GetProperty("service").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("pod").GetString()));
    }
}
