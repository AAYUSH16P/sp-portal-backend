namespace Shared;

public class UpdateCompanyRequestDto
{
    // Company
    public string CompanyName { get; set; }
    public string CompanyWebsite { get; set; }
    public string BusinessType { get; set; }
    public string CompanySize { get; set; }
    public int YearEstablished { get; set; }
    public string CompanyOverview { get; set; }
    public int? TotalProjectsExecuted { get; set; }
    public string? DomainExpertise { get; set; }

    // Address (single)
    public CompanyAddressDto Address { get; set; }

    // Contacts
    public CompanyContactDto PrimaryContact { get; set; }
    public CompanyContactDto? SecondaryContact { get; set; }

    // Certifications
    public List<string> Certifications { get; set; } = new();
}