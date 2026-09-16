using System.Net;
using System.Net.Http.Json;
using Reviews.Api.Models;
using Xunit;

namespace Reviews.Api.Tests;

public class ReviewsControllerTests(ReviewsWebApplicationFactory factory) : IClassFixture<ReviewsWebApplicationFactory>
{
    [Fact]
    public async Task Create_ThenGetByProductId_ReturnsReview()
    {
        var client = factory.CreateClient();
        var productId = Guid.NewGuid();

        var createResponse = await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(productId, 4, "Good"));
        createResponse.EnsureSuccessStatusCode();

        var getResponse = await client.GetAsync($"/api/reviews/product/{productId}");
        getResponse.EnsureSuccessStatusCode();
        var reviews = await getResponse.Content.ReadFromJsonAsync<List<Review>>();
        Assert.Single(reviews!);
    }

    [Fact]
    public async Task Create_WithInvalidRating_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(Guid.NewGuid(), 9, "bad"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ReturnsAverageRating()
    {
        var client = factory.CreateClient();
        var productId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(productId, 4, "Good"));
        await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(productId, 2, "Meh"));

        var response = await client.GetAsync($"/api/reviews/product/{productId}/summary");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ProductRatingSummary>();
        Assert.Equal(2, summary!.ReviewCount);
        Assert.Equal(3, summary.AverageRating);
    }
}
