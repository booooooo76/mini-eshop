namespace Reviews.Api.Models;

public record CreateReviewRequest(Guid ProductId, int Rating, string Comment);
