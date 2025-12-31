namespace Shared.Dtos;

public class RejectCompanyDto
{
    public Guid CompanyId { get; set; }
    public string Remark { get; set; } = string.Empty;
}