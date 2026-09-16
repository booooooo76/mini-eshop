namespace Reviews.Api.Models;

public record ProductRatingSummary(Guid ProductId, int ReviewCount, double AverageRating);
