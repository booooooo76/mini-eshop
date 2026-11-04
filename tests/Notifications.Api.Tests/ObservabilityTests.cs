using System.Net;

namespace Notifications.Api.Tests;

public class ObservabilityTests(NotificationsWebApplicationFactory factory) : IClassFixture<NotificationsWebApplicationFactory>
{
    [Fact]
    public async Task Metrics_CountsRequestsByMethodRouteAndStatus()
    {
        var client = factory.CreateClient();
        await client.GetAsync("/version");

        var metrics = await client.GetStringAsync("/metrics");

        Assert.Contains("http_requests_total{method=\"GET\",route=\"/version\",status=\"200\"}", metrics);
        Assert.Contains("http_request_duration_seconds_bucket{method=\"GET\",route=\"/version\",status=\"200\",le=", metrics);
    }

    [Fact]
    public async Task FailRate_ReturnsServerErrorButKeepsHealthProbeGreen()
    {
        using var failing = factory.WithWebHostBuilder(builder => builder.UseSetting("FAIL_RATE", "1"));
        var client = failing.CreateClient();

        var response = await client.GetAsync("/version");
        var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}
