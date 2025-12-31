using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace DynamicFormPresentation.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _service;

    public CalendarController(ICalendarService service)
    {
        _service = service;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetAvailability(
        string hostEmail,
        DateTime startUtc,
        DateTime endUtc)
    {
        var result = await _service.GetEventsAsync(
            hostEmail,
            startUtc,
            endUtc);

        return Ok(result);
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> Schedule(
        string hostEmail,
        [FromBody] ScheduleMeetingDto dto)
    {
        var result = await _service.ScheduleMeetingAsync(
            hostEmail,
            dto);

        return Ok(result);
    }
    
    
    
    [HttpPost("admin/availability")]
    public async Task<IActionResult> AdminLogin(
        string adminEmail)
    {
        await _service.SyncAdminCalendarAsync(adminEmail);
        return Ok("Calendar synced");
    }
    
    
    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] string adminEmail,
        [FromQuery] DateTime dateIst)
    {
        var slots = await _service.GetAvailableSlotsAsync(
            adminEmail, dateIst);

        return Ok(slots);
    }
}