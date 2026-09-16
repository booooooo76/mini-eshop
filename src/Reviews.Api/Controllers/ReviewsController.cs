using Microsoft.AspNetCore.Mvc;
using Reviews.Api.Models;
using Reviews.Api.Services;

namespace Reviews.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController(IReviewService reviews) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Review>>> GetAll(CancellationToken ct) =>
        Ok(await reviews.GetAllAsync(ct));

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<Review>>> GetByProductId(Guid productId, CancellationToken ct) =>
        Ok(await reviews.GetByProductIdAsync(productId, ct));

    [HttpGet("product/{productId:guid}/summary")]
    public async Task<ActionResult<ProductRatingSummary>> GetSummary(Guid productId, CancellationToken ct) =>
        Ok(await reviews.GetSummaryAsync(productId, ct));

    [HttpPost]
    public async Task<ActionResult<Review>> Create(CreateReviewRequest request, CancellationToken ct)
    {
        try
        {
            var review = await reviews.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetByProductId), new { productId = review.ProductId }, review);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Review>> Update(Guid id, UpdateReviewRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await reviews.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await reviews.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
