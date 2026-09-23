using Microsoft.EntityFrameworkCore;
using Reviews.Api.Data;
using Reviews.Api.Models;
using Reviews.Api.Services;

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

    [Fact]
    public async Task CreateAsync_WithEmptyProductId_Throws()
    {
        var db = CreateDb(nameof(CreateAsync_WithEmptyProductId_Throws));
        var service = new ReviewService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateReviewRequest(Guid.Empty, 5, "bad")));
    }

    [Fact]
    public async Task GetSummaryAsync_ComputesAverageAndCount()
    {
        var db = CreateDb(nameof(GetSummaryAsync_ComputesAverageAndCount));
        var service = new ReviewService(db);
        var productId = Guid.NewGuid();
        await service.CreateAsync(new CreateReviewRequest(productId, 4, "Good"));
        await service.CreateAsync(new CreateReviewRequest(productId, 2, "Meh"));

        var summary = await service.GetSummaryAsync(productId);

        Assert.Equal(2, summary.ReviewCount);
        Assert.Equal(3, summary.AverageRating);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoReviews_ReturnsZero()
    {
        var db = CreateDb(nameof(GetSummaryAsync_WithNoReviews_ReturnsZero));
        var service = new ReviewService(db);

        var summary = await service.GetSummaryAsync(Guid.NewGuid());

        Assert.Equal(0, summary.ReviewCount);
        Assert.Equal(0, summary.AverageRating);
    }

    [Fact]
    public async Task UpdateAsync_ChangesRatingAndComment()
    {
        var db = CreateDb(nameof(UpdateAsync_ChangesRatingAndComment));
        var service = new ReviewService(db);
        var review = await service.CreateAsync(new CreateReviewRequest(Guid.NewGuid(), 3, "Ok"));

        var updated = await service.UpdateAsync(review.Id, new UpdateReviewRequest(5, "Actually great"));

        Assert.NotNull(updated);
        Assert.Equal(5, updated!.Rating);
        Assert.Equal("Actually great", updated.Comment);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        var db = CreateDb(nameof(UpdateAsync_UnknownId_ReturnsNull));
        var service = new ReviewService(db);

        var updated = await service.UpdateAsync(Guid.NewGuid(), new UpdateReviewRequest(5, "x"));

        Assert.Null(updated);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRating_Throws()
    {
        var db = CreateDb(nameof(UpdateAsync_WithInvalidRating_Throws));
        var service = new ReviewService(db);
        var review = await service.CreateAsync(new CreateReviewRequest(Guid.NewGuid(), 3, "Ok"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(review.Id, new UpdateReviewRequest(0, "bad")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesReview()
    {
        var db = CreateDb(nameof(DeleteAsync_RemovesReview));
        var service = new ReviewService(db);
        var review = await service.CreateAsync(new CreateReviewRequest(Guid.NewGuid(), 3, "Ok"));

        var deleted = await service.DeleteAsync(review.Id);

        Assert.True(deleted);
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var db = CreateDb(nameof(DeleteAsync_UnknownId_ReturnsFalse));
        var service = new ReviewService(db);

        var deleted = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
