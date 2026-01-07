using Shared;

namespace DynamicFormRepo.DynamicFormRepoInterface;

public interface ICompanyChangeRequestRepository
{
    Task CreateAsync(CompanyChangeRequestDto dto);
    Task<List<CompanyChangeRequestViewDto>> GetPendingAsync();
    Task ApproveAsync(Guid requestId, Guid adminId);
    Task RejectAsync(Guid requestId, string remark, Guid adminId);
    
    Task<Guid?> GetCompanyIdByPrimaryEmailAsync(string email);

    Task SaveResetTokenAsync(
        Guid companyId,
        string token,
        DateTime expiresAt);

    Task<CompanyResetProjection?> GetByResetTokenAsync(string token);

    Task UpdatePasswordAsync(Guid companyId, string passwordHash);
}
