namespace Shared.Dtos;

public class CompanyLoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public Guid CompanyId { get; set; } = Guid.Empty!;
}
