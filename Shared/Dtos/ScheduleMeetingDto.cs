namespace Shared;

public class ScheduleMeetingDto
{
    public Guid CompanyId { get; set; } 
    public string Subject { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public List<string> AttendeeEmails { get; set; } = new();
}
