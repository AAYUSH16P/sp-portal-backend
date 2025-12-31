using System;
using System.Collections.Generic;

namespace FinancialManagementDataAccess.Models;

public partial class ProjectRole
{
    public Guid Id { get; set; }

    public Guid? ProjectId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? RoleSpecification { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();

    public virtual ICollection<WorkArrangement> WorkArrangements { get; set; } = new List<WorkArrangement>();
}
