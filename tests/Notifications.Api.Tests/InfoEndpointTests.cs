using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Notifications.Api.Tests;

public class InfoEndpointTests(NotificationsWebApplicationFactory factory) : IClassFixture<NotificationsWebApplicationFactory>
{
    [Fact]
    public async Task Info_ReturnsServiceNameVersionAndPod()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("notifications-api", body.GetProperty("service").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("pod").GetString()));
    }

    [Fact]
    public async Task Version_ReturnsServiceNameAndSameVersionAsInfo()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var info = await client.GetFromJsonAsync<JsonElement>("/info");
        Assert.Equal("notifications-api", body.GetProperty("service").GetString());
        Assert.Equal(info.GetProperty("version").GetString(), body.GetProperty("version").GetString());
    }
}
