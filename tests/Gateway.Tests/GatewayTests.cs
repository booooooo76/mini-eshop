using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Tests;

public class GatewayTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPath_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/no-such-service/anything");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("catalog", "http://catalog-api:8080")]
    [InlineData("orders", "http://orders-api:8080")]
    [InlineData("notifications", "http://notifications-api:8080")]
    [InlineData("reviews", "http://reviews-api:8080")]
    public void ProxyConfig_RoutesPrefixToServiceAndStripsPrefix(string prefix, string expectedAddress)
    {
        var config = factory.Services.GetRequiredService<IProxyConfigProvider>().GetConfig();

        var route = Assert.Single(config.Routes, r => r.RouteId == $"{prefix}-route");
        Assert.Equal($"/{prefix}/{{**catch-all}}", route.Match.Path);
        Assert.Contains(route.Transforms!, t => t.TryGetValue("PathRemovePrefix", out var value) && value == $"/{prefix}");

        var cluster = Assert.Single(config.Clusters, c => c.ClusterId == route.ClusterId);
        Assert.Equal(expectedAddress, cluster.Destinations!.Single().Value.Address);
    }
}
