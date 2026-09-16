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

        ValidateRating(request.Rating);

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

    public async Task<Review?> UpdateAsync(Guid id, UpdateReviewRequest request, CancellationToken ct = default)
    {
        ValidateRating(request.Rating);

        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (review is null)
        {
            return null;
        }

        review.Rating = request.Rating;
        review.Comment = request.Comment;
        await db.SaveChangesAsync(ct);
        return review;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (review is null)
        {
            return false;
        }

        db.Reviews.Remove(review);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void ValidateRating(int rating)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }
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
