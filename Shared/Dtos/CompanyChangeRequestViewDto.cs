namespace Shared;

public class CompanyChangeRequestViewDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; }

    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }

    public string Reason { get; set; }
    public string Status { get; set; }

    public DateTime RequestedAt { get; set; }
}