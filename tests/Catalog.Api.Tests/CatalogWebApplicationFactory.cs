using Catalog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Catalog.Api.Tests;

public class CatalogWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddDbContext aggregates every IDbContextOptionsConfiguration<T> registered so
            // far, so the Npgsql config from Program.cs must be removed too, not just replaced.
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CatalogDbContext>>();
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseInMemoryDatabase("catalog-integration-tests"));

            services.RemoveAll<IHostedService>();

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }
}
