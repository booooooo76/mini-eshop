using System.Text;
using Reviews.Api.Models;

namespace Reviews.Api.Services;

public interface IReviewService
{
    Task<Review> CreateAsync(CreateReviewRequest request, CancellationToken ct = default);
    Task<Review?> UpdateAsync(Guid id, UpdateReviewRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<ProductRatingSummary> GetSummaryAsync(Guid productId, CancellationToken ct = default);
}
