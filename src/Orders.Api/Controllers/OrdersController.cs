using Microsoft.AspNetCore.Mvc;
using Orders.Api.Models;
using Orders.Api.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Order>>> GetAll(CancellationToken ct) =>
        Ok(await orders.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetById(Guid id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        try
        {
            var order = await orders.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
