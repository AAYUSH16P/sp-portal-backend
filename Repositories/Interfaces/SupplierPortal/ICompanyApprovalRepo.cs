using FinancialManagementDataAccess.Models;
using Shared;
using Shared.Dtos;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface ICompanyApprovalRepo
{
    Task<CompanyLoginDataDto?> GetLoginDataByCompanyIdAsync(Guid companyId);
    Task ApproveCompanyAsync(Guid companyId, string passwordHash);
    Task RejectCompanyAsync(Guid companyId, string remark);
    Task<CompanyLoginDataDto?> GetLoginDataAsync(string email);
    Task<CompanyDto> GetDetailsAsync(Guid companyId);
    Task<IEnumerable<CompanyDto>> GetAllSuppliersAsync();
    Task<(string Email, string ContactName)?> GetPrimaryContactAsync(Guid companyId);
    Task<IEnumerable<SupplierCapacity>> GetSupplierRejectedAsync(Guid companyId);
    Task<IEnumerable<SupplierCapacity>> GetHrRejectedAsync(Guid companyId);
    Task<bool> MarkAcknowledgedAsync(Guid companyId);

}