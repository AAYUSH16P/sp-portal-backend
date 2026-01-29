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

    public AdminNotificationController(INotificationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateNotificationDto dto)
    {
        int adminId = 1;

        await _service.CreateAsync(dto, adminId);
        return Ok(new { message = "Notification created successfully" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UpdateNotificationDto dto)
    {
        int adminId = 1;
        dto.NotificationId = id;

        await _service.UpdateAsync(dto, adminId);
        return Ok(new { message = "Notification updated successfully" });
    }

    [HttpPost("{id}/send")]
    public async Task<IActionResult> Send(int id)
    {
        int adminId = 1;

        await _service.SendAsync(id, adminId);
        return Ok(new { message = "Notification sent successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int adminId = 1;

        await _service.DeleteAsync(id, adminId);
        return Ok(new { message = "Notification deleted successfully" });
    }
}