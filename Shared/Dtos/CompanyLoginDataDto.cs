namespace Shared.Dtos;

public class CompanyLoginDataDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsSlaSigned { get; set; }
}
