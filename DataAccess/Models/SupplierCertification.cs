namespace FinancialManagementDataAccess.Models;

public class SupplierCertification
{
    public Guid Id { get; set; }
    public Guid SupplierCapacityId { get; set; }
    public string CertificationName { get; set; }
}
