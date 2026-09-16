using Reviews.Api.Models;

namespace Reviews.Api.Services;

public interface IReviewService
{
    Task<Review> CreateAsync(CreateReviewRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
}
