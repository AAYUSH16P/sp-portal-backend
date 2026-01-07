namespace Shared.Dtos;

public class CompanyLoginDataDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsSlaSigned { get; set; }
    public string IsPasswordChanged {get; set;} = string.Empty;
    public bool IsAcknowledged { get; set; }
    public DateTime? NextMeetingAt { get; set; }
}
