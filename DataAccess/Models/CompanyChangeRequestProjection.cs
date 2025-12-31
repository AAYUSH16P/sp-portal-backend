namespace FinancialManagementDataAccess.Models;

public class CompanyChangeRequestProjection
{
    public Guid CompanyId { get; set; }
    public string FieldKey { get; set; }
    public string NewValue { get; set; }
}
