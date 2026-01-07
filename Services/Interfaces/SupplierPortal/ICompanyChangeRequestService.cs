using Shared;

public interface ICompanyChangeRequestService
{
    Task SubmitAsync(CompanyChangeRequestDto dto);
    Task<List<CompanyChangeRequestViewDto>> GetPendingAsync();
    Task ApproveAsync(Guid requestId, Guid adminId);
    Task RejectAsync(Guid requestId, string remark, Guid adminId);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
}