using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((_, handler) => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(5));

var app = builder.Build();

app.UseObservability();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/info", () => Results.Ok(new
{
    service = "gateway",
    version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev",
    pod = Environment.MachineName
}));
app.MapGet("/version", () => Results.Ok(new
{
    service = "gateway",
    version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev"
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        foreach (var endpoint in app.Configuration.GetSection("SwaggerAggregator:Endpoints").GetChildren())
        {
            options.SwaggerEndpoint(endpoint["Url"], endpoint["Name"]);
        }
    });
}

app.MapReverseProxy();

app.Run();

public partial class Program
{
}
