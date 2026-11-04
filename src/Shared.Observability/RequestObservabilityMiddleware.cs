using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Shared.Observability;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger,
    IConfiguration configuration)
{
    private static readonly string[] LabelNames = ["method", "route", "status"];

    private static readonly Counter Requests = Metrics.CreateCounter(
        "http_requests_total",
        "HTTP requests handled by the service.",
        new CounterConfiguration { LabelNames = LabelNames });

    private static readonly Histogram Duration = Metrics.CreateHistogram(
        "http_request_duration_seconds",
        "Time spent handling HTTP requests.",
        new HistogramConfiguration { LabelNames = LabelNames, Buckets = Histogram.ExponentialBuckets(0.005, 2, 12) });

    private readonly double failRate = configuration.GetValue<double>("FAIL_RATE");

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await next(context);
            return;
        }

        var isProbe = context.Request.Path.StartsWithSegments("/health");
        var started = Stopwatch.GetTimestamp();
        var crashed = false;
        try
        {
            if (!isProbe && failRate > 0 && Random.Shared.NextDouble() < failRate)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
            else
            {
                await next(context);
            }
        }
        catch
        {
            crashed = true;
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            var status = crashed ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            var method = context.Request.Method;
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            var statusLabel = status.ToString(CultureInfo.InvariantCulture);

            Requests.WithLabels(method, route, statusLabel).Inc();
            Duration.WithLabels(method, route, statusLabel).Observe(elapsed.TotalSeconds);

            if (!isProbe)
            {
                var level = status >= 500 ? LogLevel.Error : status >= 400 ? LogLevel.Warning : LogLevel.Information;
                logger.Log(level, "HTTP {Method} {Route} responded {Status} in {DurationMs} ms",
                    method, route, status, Math.Round(elapsed.TotalMilliseconds, 1));
            }
        }
    }
}
