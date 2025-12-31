using Shared;
using Shared.Dtos;

namespace DynamicFormService.DynamicFormServiceInterface;

public interface ICompanyApprovalService
{
    Task ApproveAsync(Guid companyId);
    Task RejectAsync(Guid companyId, string remark);
    Task<LoginResponseDto> LoginAsync(CompanyLoginDto dto);
    Task<CompanyDto> GetDetailsAsync(Guid companyId);
    Task<IEnumerable<CompanyDto>> GetPendingCompaniesAsync();

}