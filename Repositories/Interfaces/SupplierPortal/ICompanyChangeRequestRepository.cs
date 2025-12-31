using Shared;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface ICompanyChangeRequestRepository
{
    Task CreateAsync(CompanyChangeRequestDto dto);
    Task<List<CompanyChangeRequestViewDto>> GetPendingAsync();
    Task ApproveAsync(Guid requestId, Guid adminId);
    Task RejectAsync(Guid requestId, string remark, Guid adminId);
}
