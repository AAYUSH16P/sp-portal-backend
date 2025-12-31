namespace Shared;

public class CompanyRegistrationRequestDto
{
    // Company
    public string CompanyName { get; set; }
    public string CompanyWebsite { get; set; }
    public string BusinessType { get; set; }
    public string CompanySize { get; set; }
    public int YearEstablished { get; set; }
    public string CompanyOverview { get; set; }

    public int? ProjectExecuted { get; set; }
    public string DomainExpertise { get; set; }

    // Address
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }

    // Primary Contact
    public string PrimaryContactName { get; set; }
    public string PrimaryContactRole { get; set; }
    public string PrimaryContactEmail { get; set; }
    public string PrimaryContactPhone { get; set; }

    // Secondary Contact (optional)
    public string SecondaryContactName { get; set; }
    public string SecondaryContactRole { get; set; }
    public string SecondaryContactEmail { get; set; }
    public string SecondaryContactPhone { get; set; }

    // Certifications
    public List<string> Certifications { get; set; } = new();
}
