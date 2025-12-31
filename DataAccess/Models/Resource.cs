using System;
using System.Collections.Generic;

namespace FinancialManagementDataAccess.Models;

public partial class Resource
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? ProjectRoleId { get; set; }

    public Guid? WorkArrangementId { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public DateOnly? Dob { get; set; }

    public int? Age { get; set; }

    public string? BloodGroup { get; set; }

    public string? ResumeUrl { get; set; }

    public string? ResourceType { get; set; }

    public decimal? CirRate { get; set; }

    public decimal? AcrRate { get; set; }

    public decimal? AcrCommission { get; set; }

    public int? ExpectedWorkingDays { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? SkillsExpertise { get; set; }

    public string? PastProjects { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ProjectRole? ProjectRole { get; set; }

    public virtual ICollection<ResourceWorkArrangementOverride> ResourceWorkArrangementOverrides { get; set; } = new List<ResourceWorkArrangementOverride>();

    public virtual User? User { get; set; }

    public virtual WorkArrangement? WorkArrangement { get; set; }
}
