using Shared.Enum;

namespace FinancialManagementDataAccess.Models;

public class SupplierCapacity
{
    public Guid Id { get; set; }
    public string CompanyEmployeeId { get; set; }
    public DateOnly WorkingSince { get; set; }
    public string CompanyName { get; set; }

    public decimal CTC { get; set; }
    public string JobTitle { get; set; }
    public string Role { get; set; }
    public string Gender { get; set; }
    public string Location { get; set; }
    public decimal TotalExperience { get; set; }
    public string TechnicalSkills { get; set; }
    public string Tools { get; set; }
    public int NumberOfProjects { get; set; }
    public string EmployerNote { get; set; }

    public Guid? CompanyId { get; set; }
    public bool IsRefered { get; set; }
    public SupplierStatus Status { get; set; }
    public ApprovalStage ApprovalStage { get; set; }
    public string Remark { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<SupplierCertification> Certifications { get; set; }
}
