using Microsoft.EntityFrameworkCore;
using Notifications.Api.Data;
using Notifications.Api.Messaging;
using Notifications.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Notifications")));

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationsDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Notifications.Api v1"));
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
