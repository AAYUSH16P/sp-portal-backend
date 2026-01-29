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

    private int SupplierId => int.Parse(User.FindFirst("SupplierId").Value);

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetNotificationsAsync(SupplierId));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetByIdAsync(id, SupplierId));

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _service.MarkAsReadAsync(id, SupplierId);
        return Ok();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
        => Ok(await _service.GetUnreadCountAsync(SupplierId));

    [HttpGet("{id}/attachment")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await _service.GetAttachmentAsync(id);
        return File(file.content, file.mime, file.name);
    }
}