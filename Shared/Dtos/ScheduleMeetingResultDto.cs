namespace Shared;

public class ScheduleMeetingResultDto
{
    public string EventId { get; set; }
    public string? JoinUrl { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}
