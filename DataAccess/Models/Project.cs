using System;
using System.Collections.Generic;

namespace FinancialManagementDataAccess.Models;

public partial class Project
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string TeamName { get; set; } = null!;

    public string? ClientName { get; set; }

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ProjectRole> ProjectRoles { get; set; } = new List<ProjectRole>();

    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();

    public virtual User? User { get; set; }
}
