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
}
