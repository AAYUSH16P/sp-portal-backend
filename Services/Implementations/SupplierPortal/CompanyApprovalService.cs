using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using FinancialManagementDataAccess.Models;
using Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Dtos;
using Shared.Enums;

namespace DynamicFormService.DynamicFormServiceImplementation;

public class CompanyApprovalService : ICompanyApprovalService
{
    private readonly ICompanyApprovalRepo _repo;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly ILogger<CompanyApprovalService> _logger;


    public CompanyApprovalService(
        ICompanyApprovalRepo repo,
        ITemplateRenderer templateRenderer,
        IEmailSender emailSender,
        IJwtTokenGenerator jwtGenerator,
        ILogger<CompanyApprovalService> logger)
    {
        _repo = repo;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _jwtGenerator = jwtGenerator;
        _logger = logger;
    }

    public async Task ApproveAsync(Guid companyId)
    {
        _logger.LogInformation("Starting company approval. CompanyId: {CompanyId}", companyId);

        var plainPassword = PasswordGenerator.Generate();
        var passwordHash = PasswordHasher.Hash(plainPassword);

        await _repo.ApproveCompanyAsync(companyId, passwordHash);

        _logger.LogInformation(
            "Company approved successfully in DB. CompanyId: {CompanyId}",
            companyId
        );

        var contact = await _repo.GetPrimaryContactAsync(companyId);

        if (contact == null)
        {
            _logger.LogWarning(
                "Approval completed but primary contact record NOT FOUND. CompanyId: {CompanyId}",
                companyId
            );
            return;
        }

        var (email, contactName) = contact.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning(
                "Approval completed but primary contact email is MISSING. CompanyId: {CompanyId}, ContactName: {ContactName}",
                companyId,
                contactName
            );
            return;
        }

        _logger.LogInformation(
            "Sending approval email. CompanyId: {CompanyId}, Email: {Email}",
            companyId,
            email
        );

        var body = _templateRenderer.Render(
            EmailTemplateType.SupplierApproved,
            new Dictionary<string, string>
            {
                ["SupplierContactName"] = contactName ?? "Supplier",
                ["LoginEmail"] = email,
                ["PortalLink"] = "https://supplier-portal-frontend-production.up.railway.app/login",
                ["TemporaryPassword"] = plainPassword
            },
            out var subject
        );

        await _emailSender.SendAsync(email, subject, body,true);

        _logger.LogInformation(
            "Approval email sent successfully. CompanyId: {CompanyId}, Email: {Email}",
            companyId,
            email
        );
    }


    public async Task RejectAsync(Guid companyId, string remark)
    {
        await _repo.RejectCompanyAsync(companyId, remark);

        var contact = await _repo.GetPrimaryContactAsync(companyId);

        if (contact == null || string.IsNullOrWhiteSpace(contact.Value.Email))
        {
            Console.WriteLine($"Reject email missing for company {companyId}");
            return;
        }

        var (email, contactName) = contact.Value;
        var body = _templateRenderer.Render(
            EmailTemplateType.SupplierRejected,
            new Dictionary<string, string>
            {
                ["SupplierContactName"] = contactName,
                ["Remark"] = remark
            },
            out var subject);

        await _emailSender.SendAsync(email, subject, body,false);
}
    
    
    public async Task<LoginResponseDto> LoginAsync(CompanyLoginDto dto)
    {
        _logger.LogInformation("🔐 Login attempt started for Email: {Email}", dto.Email);

        var data = await _repo.GetLoginDataAsync(dto.Email);

        if (data == null)
        {
            _logger.LogWarning("❌ Login failed: No user found for Email: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        _logger.LogInformation(
            "✅ User found. CompanyId: {CompanyId}, IsSlaSigned: {IsSlaSigned}",
            data.CompanyId,
            data.IsSlaSigned
        );

        if (!PasswordHasher.Verify(dto.Password, data.PasswordHash))
        {
            _logger.LogWarning(
                "❌ Login failed: Password mismatch for Email: {Email}, CompanyId: {CompanyId}",
                dto.Email,
                data.CompanyId
            );

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        _logger.LogInformation(
            "🔑 Password verified successfully for CompanyId: {CompanyId}",
            data.CompanyId
        );

        var token = _jwtGenerator.Generate(
            data.CompanyId,
            data.IsSlaSigned,
            dto.Email,
            data.CompanyName,
            data.IsPasswordChanged,
            data.IsAcknowledged,
            data.NextMeetingAt,
            out var expiresAt
        );

        _logger.LogInformation(
            "🎟️ JWT generated successfully for CompanyId: {CompanyId}, ExpiresAt: {ExpiresAt}",
            data.CompanyId,
            expiresAt
        );

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    
    
    public async Task<CompanyDto> GetDetailsAsync(Guid companyId)
    {
        return await _repo.GetDetailsAsync(companyId);
    }
    
    public async Task<IEnumerable<CompanyDto>> GetPendingCompaniesAsync()
    {
        return await _repo.GetAllSuppliersAsync();
    }
    
    public Task<IEnumerable<SupplierCapacity>> GetSupplierRejectedAsync(Guid companyId)
        => _repo.GetSupplierRejectedAsync(companyId);

    public Task<IEnumerable<SupplierCapacity>> GetHrRejectedAsync(Guid companyId)
        => _repo.GetHrRejectedAsync(companyId);


    public async Task<bool> AcknowledgeCompanyAsync(Guid companyId)
    {
        return await _repo.MarkAcknowledgedAsync(companyId);
    }
}