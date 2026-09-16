using Microsoft.EntityFrameworkCore;
using Reviews.Api.Data;
using Reviews.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<ReviewsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Reviews")));

builder.Services.AddScoped<IReviewService, ReviewService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Reviews.Api v1"));
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program
{
}
