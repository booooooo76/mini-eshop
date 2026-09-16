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

    [Fact]
    public async Task Update_ThenGet_ReturnsUpdatedReview()
    {
        var client = factory.CreateClient();
        var productId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(productId, 3, "Ok"));
        var created = await createResponse.Content.ReadFromJsonAsync<Review>();

        var updateResponse = await client.PutAsJsonAsync($"/api/reviews/{created!.Id}", new UpdateReviewRequest(5, "Now great"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<Review>();
        Assert.Equal(5, updated!.Rating);
        Assert.Equal("Now great", updated.Comment);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/reviews/{Guid.NewGuid()}", new UpdateReviewRequest(5, "x"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetByProductId_ReturnsEmpty()
    {
        var client = factory.CreateClient();
        var productId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(productId, 3, "Ok"));
        var created = await createResponse.Content.ReadFromJsonAsync<Review>();

        var deleteResponse = await client.DeleteAsync($"/api/reviews/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/reviews/product/{productId}");
        var remaining = await getResponse.Content.ReadFromJsonAsync<List<Review>>();
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/reviews/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
