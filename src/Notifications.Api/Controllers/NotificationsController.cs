using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Models;
using Notifications.Api.Services;

namespace Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(INotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Notification>>> GetAll(CancellationToken ct) =>
        Ok(await notifications.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Notification>> GetById(Guid id, CancellationToken ct)
    {
        var notification = await notifications.GetByIdAsync(id, ct);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var updated = await notifications.MarkAsReadAsync(id, ct);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await notifications.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
