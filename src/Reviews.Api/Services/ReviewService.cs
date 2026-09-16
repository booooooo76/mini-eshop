using Microsoft.EntityFrameworkCore;
using Reviews.Api.Data;
using Reviews.Api.Models;

namespace Reviews.Api.Services;

public class ReviewService(ReviewsDbContext db) : IReviewService
{
    public async Task<Review> CreateAsync(CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new ArgumentException("A valid productId is required.");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }

        var review = new Review
        {
            ProductId = request.ProductId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        return review;
    }

    public async Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default) =>
        await db.Reviews.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken ct = default) =>
        await db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<ProductRatingSummary> GetSummaryAsync(Guid productId, CancellationToken ct = default)
    {
        var ratings = await db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .Select(r => r.Rating)
            .ToListAsync(ct);

        var average = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 2);
        return new ProductRatingSummary(productId, ratings.Count, average);
    }
}
