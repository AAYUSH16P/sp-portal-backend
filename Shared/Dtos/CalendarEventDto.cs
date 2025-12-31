namespace Shared;

public class CalendarEventDto
{
    public string Subject { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? JoinUrl { get; set; }
}
