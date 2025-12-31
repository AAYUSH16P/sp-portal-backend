namespace Shared;

public class CompanyDto
{
    
    public Guid Id { get; set; }
    public string CompanyName { get; set; }
    public string CompanyWebsite { get; set; }
    public string BusinessType { get; set; }
    public string CompanySize { get; set; }
    public int YearEstablished { get; set; }
    public string CompanyOverview { get; set; }
    public bool IsApproved { get; set; }
    public string Remark { get; set; }
    public int? TotalProjectsExecuted { get; set; }
    public string? DomainExpertise { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSlaSigned { get; set; }
    public List<CompanyContact> Contacts { get; set; } = new();
    public List<CompanyAddress> Addresses { get; set; } = new();
    public List<CompanyCertification> Certifications { get; set; } = new();
}
