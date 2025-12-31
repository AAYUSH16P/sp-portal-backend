namespace Shared;

public class AdminCalendarEventDto
{
    public string EventId { get; set; } = null!;
    public string Subject { get; set; } = "";
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
}
