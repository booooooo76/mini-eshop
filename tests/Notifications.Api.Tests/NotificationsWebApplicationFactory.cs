using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Notifications.Api.Data;

namespace Notifications.Api.Tests;

public class NotificationsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NotificationsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<NotificationsDbContext>>();
            services.AddDbContext<NotificationsDbContext>(options =>
                options.UseInMemoryDatabase("notifications-integration-tests"));

            services.RemoveAll<IHostedService>();
        });
    }
}
