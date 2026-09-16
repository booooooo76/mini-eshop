using Catalog.Api.Models;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll(CancellationToken ct) =>
        Ok(await products.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product, CancellationToken ct)
    {
        var created = await products.CreateAsync(product, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
