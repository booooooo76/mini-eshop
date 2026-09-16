using Microsoft.EntityFrameworkCore;
using Reviews.Api.Data;
using Reviews.Api.Models;
using Reviews.Api.Services;
using Xunit;

namespace Reviews.Api.Tests;

public class ReviewServiceTests
{
    private static ReviewsDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ReviewsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ReviewsDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsReview()
    {
        var db = CreateDb(nameof(CreateAsync_PersistsReview));
        var service = new ReviewService(db);
        var productId = Guid.NewGuid();

        var review = await service.CreateAsync(new CreateReviewRequest(productId, 5, "Great product"));

        var byProduct = await service.GetByProductIdAsync(productId);
        Assert.Single(byProduct);
        Assert.Equal(review.Id, byProduct[0].Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_WithInvalidRating_Throws(int rating)
    {
        var db = CreateDb($"{nameof(CreateAsync_WithInvalidRating_Throws)}_{rating}");
        var service = new ReviewService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateReviewRequest(Guid.NewGuid(), rating, "bad")));
    }
}
