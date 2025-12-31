using System;
using System.Collections.Generic;

namespace FinancialManagementDataAccess.Models;

public partial class User
{
    public Guid Id { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Name { get; set; }

    public virtual User1 IdNavigation { get; set; } = null!;

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
