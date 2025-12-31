namespace Shared;

public class CompanyChangeRequestDto
{
    public Guid CompanyId { get; set; }
    public Guid RequestedBy { get; set; }   // userId from token
    public string FieldKey { get; set; }
    public string FieldName { get; set; }   // optional
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public string Reason { get; set; }
}
