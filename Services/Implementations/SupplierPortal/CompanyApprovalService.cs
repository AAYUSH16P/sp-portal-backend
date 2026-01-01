using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using Hangfire;
using Infrastructure.Security;
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

    public CompanyApprovalService(
        ICompanyApprovalRepo repo,
        ITemplateRenderer templateRenderer,
        IEmailSender emailSender,
        IJwtTokenGenerator jwtGenerator)
    {
        _repo = repo;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _jwtGenerator = jwtGenerator;
    }

    public async Task ApproveAsync(Guid companyId)
    {
        var plainPassword = PasswordGenerator.Generate();

        // 2. Hash password
        var passwordHash = PasswordHasher.Hash(plainPassword);

        // 3. Approve company
        await _repo.ApproveCompanyAsync(companyId, passwordHash);

        // 4. Fetch primary contact
        var contact = await _repo.GetPrimaryContactAsync(companyId);

        if (contact == null || string.IsNullOrWhiteSpace(contact.Value.Email))
        {
            Console.WriteLine($"Approval done but email missing for company {companyId}");
            return;
        }

        var (email, contactName) = contact.Value;

        // 5. Prepare email
        var body = _templateRenderer.Render(
            EmailTemplateType.SupplierApproved,
            new Dictionary<string, string>
            {
                ["SupplierContactName"] = contactName ?? "Supplier",
                ["LoginEmail"] = email,
                ["PortalLink"] = "https://supplier-portal-frontend-production.up.railway.app/login",
                ["TemporaryPassword"] = plainPassword
            },
            out var subject);

        BackgroundJob.Enqueue<IEmailSender>(sender =>
            sender.SendAsync(email, subject, body)
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

        BackgroundJob.Enqueue<IEmailSender>(sender =>
            sender.SendAsync(email, subject, body)
        );
    }
    
    
    public async Task<LoginResponseDto> LoginAsync(CompanyLoginDto dto)
    {
        var data = await _repo.GetLoginDataAsync(dto.Email);

        if (data == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, data.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = _jwtGenerator.Generate(data.CompanyId,data.IsSlaSigned, dto.Email, data.CompanyName,out var expiresAt);

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

}