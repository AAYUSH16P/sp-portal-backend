using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using Infrastructure.Security;
using Shared;
using Shared.Dtos;
using Shared.Enums;

public class CompanyChangeRequestService : ICompanyChangeRequestService
{
    private readonly ICompanyChangeRequestRepository _repo;
    private readonly IEmailService _emailService;

    public CompanyChangeRequestService(ICompanyChangeRequestRepository repo,IEmailService emailService)
    {
        _repo = repo;
        _emailService = emailService;
    }

    public Task SubmitAsync(CompanyChangeRequestDto dto)
        => _repo.CreateAsync(dto);

    public Task<List<CompanyChangeRequestViewDto>> GetPendingAsync()
        => _repo.GetPendingAsync();

    public Task ApproveAsync(Guid requestId, Guid adminId)
        => _repo.ApproveAsync(requestId, adminId);

    public Task RejectAsync(Guid requestId, string remark, Guid adminId)
        => _repo.RejectAsync(requestId, remark, adminId);
    
    public async Task ForgotPasswordAsync(string email)
    {
        var companyId = await _repo.GetCompanyIdByPrimaryEmailAsync(email);

        // 🔐 SECURITY: always return success
        if (companyId == null)
            return;

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        await _repo.SaveResetTokenAsync(companyId.Value, token, expiresAt);

        // ✅ UPDATED FRONTEND LINK
        var resetLink =
            $"https://supplier-portal-frontend-production.up.railway.app/reset-password?token={token}";

        await _emailService.SendAsync(new SendEmailRequestDto
        {
            To = email,
            TemplateType = EmailTemplateType.ForgotPassword,
            Variables = new Dictionary<string, string>
            {
                ["ResetLink"] = resetLink,
                ["ExpiryMinutes"] = "30"
            }
        });
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var record = await _repo.GetByResetTokenAsync(token);

        if (record == null || record.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired token");

        var hash = PasswordHasher.Hash(newPassword);

        await _repo.UpdatePasswordAsync(record.CompanyId, hash);

    }
}