using DynamicFormRepo.DynamicFormRepoInterface;
using Shared;

public class CompanyChangeRequestService : ICompanyChangeRequestService
{
    private readonly ICompanyChangeRequestRepository _repo;

    public CompanyChangeRequestService(ICompanyChangeRequestRepository repo)
    {
        _repo = repo;
    }

    public Task SubmitAsync(CompanyChangeRequestDto dto)
        => _repo.CreateAsync(dto);

    public Task<List<CompanyChangeRequestViewDto>> GetPendingAsync()
        => _repo.GetPendingAsync();

    public Task ApproveAsync(Guid requestId, Guid adminId)
        => _repo.ApproveAsync(requestId, adminId);

    public Task RejectAsync(Guid requestId, string remark, Guid adminId)
        => _repo.RejectAsync(requestId, remark, adminId);
}