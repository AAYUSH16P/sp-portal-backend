using Shared;
using Shared.Dtos;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface ICompanyApprovalRepo
{
    Task ApproveCompanyAsync(Guid companyId, string passwordHash);
    Task RejectCompanyAsync(Guid companyId, string remark);
    Task<(string Email, string ContactName)> GetPrimaryContactAsync(Guid companyId);
    Task<CompanyLoginDataDto?> GetLoginDataAsync(string email);
    Task<CompanyDto> GetDetailsAsync(Guid companyId);
    Task<IEnumerable<CompanyDto>> GetAllSuppliersAsync();
}