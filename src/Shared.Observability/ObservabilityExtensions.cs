using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Prometheus;

namespace Shared.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.FormatterName = JsonLineFormatter.FormatterName);
        builder.Logging.AddConsoleFormatter<JsonLineFormatter, ConsoleFormatterOptions>();
        return builder;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<RequestObservabilityMiddleware>();
        app.MapMetrics();
        return app;
    }
}
