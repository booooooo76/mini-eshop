using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reviews.Api.Data;

namespace Reviews.Api.Tests;

public class ReviewsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReviewsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ReviewsDbContext>>();
            services.AddDbContext<ReviewsDbContext>(options =>
                options.UseInMemoryDatabase("reviews-integration-tests"));
        });
    }
}
