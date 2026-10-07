var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/info", () => Results.Ok(new
{
    service = "gateway",
    version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev",
    pod = Environment.MachineName
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
