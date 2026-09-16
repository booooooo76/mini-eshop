using Microsoft.EntityFrameworkCore;
using Reviews.Api.Models;

namespace Reviews.Api.Data;

public class ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();
}
