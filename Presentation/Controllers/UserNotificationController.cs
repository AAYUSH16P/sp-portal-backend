using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/user/notifications")]
public class UserNotificationController : ControllerBase
{
    private readonly IUserNotificationService _service;

    public UserNotificationController(IUserNotificationService service)
    {
        _service = service;
    }

    // ✅ supplierId passed explicitly (UUID)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid supplierId)
        => Ok(await _service.GetNotificationsAsync(supplierId));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, [FromQuery] Guid supplierId)
        => Ok(await _service.GetByIdAsync(id, supplierId));

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id, [FromQuery] Guid supplierId)
    {
        await _service.MarkAsReadAsync(id, supplierId);
        return Ok();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount([FromQuery] Guid supplierId)
        => Ok(await _service.GetUnreadCountAsync(supplierId));

    [HttpGet("{id}/attachment")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await _service.GetAttachmentAsync(id);
        return File(file.content, file.mime, file.name);
    }
}