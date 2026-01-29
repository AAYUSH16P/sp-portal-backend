using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers.Admin;

[ApiController]
[Route("api/admin/notifications")]
//[Authorize(Roles = "Admin")]
public class AdminNotificationController : ControllerBase
{
    private readonly INotificationService _service;

    // ✅ Fixed Admin GUID (POC purpose)
    private static readonly Guid AdminId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public AdminNotificationController(INotificationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateNotificationDto dto)
    {
        await _service.CreateAsync(dto, AdminId);
        return Ok(new { message = "Notification created successfully" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateNotificationDto dto)
    {
        dto.NotificationId = id;
        await _service.UpdateAsync(dto, AdminId);
        return Ok(new { message = "Notification updated successfully" });
    }

    [HttpPost("{id}/send")]
    public async Task<IActionResult> Send(int id)
    {
        await _service.SendAsync(id, AdminId);
        return Ok(new { message = "Notification sent successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, AdminId);
        return Ok(new { message = "Notification deleted successfully" });
    }
}