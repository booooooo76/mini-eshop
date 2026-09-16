using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Api.Data;
using Orders.Api.Messaging;

namespace Orders.Api.Tests;

public class OrdersWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddDbContext aggregates every IDbContextOptionsConfiguration<T> registered so
            // far, so the Npgsql config from Program.cs must be removed too, not just replaced.
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<OrdersDbContext>>();
            services.AddDbContext<OrdersDbContext>(options =>
                options.UseInMemoryDatabase("orders-integration-tests"));

            services.RemoveAll<IOrderPublisher>();
            services.AddSingleton<IOrderPublisher, FakeOrderPublisher>();
        });
    }
}
