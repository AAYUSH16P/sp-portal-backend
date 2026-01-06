using FinancialManagementDataAccess.Models;
using Shared;
using Shared.Enum;

namespace DynamicFormRepo.DynamicFormRepoInterface
{
    public interface ISupplierRepoInterface
    {
        Task<IEnumerable<SupplierResourceDto>> GetEligibleSuppliersAsync();
        Task<Guid> SubmitCompanyAsync(CompanyRegistrationRequestDto request);
        Task<string?> GetPasswordHashAsync(Guid companyId);

        Task<long> CreateUploadAsync(
            Guid companyId,
            int uploadedBy,
            string fileName);
        Task SaveFileAsync(long uploadId, byte[] content, long size);
        Task<byte[]> GetFileAsync(long uploadId);
        Task DeleteFileAsync(long uploadId);
        Task InsertErrorAsync(long uploadId, int rowNumber, string reason);
        Task UpdateUploadAsync(long uploadId, int total, int success, int failure, string status);

        Task InsertSupplierResourcesBatchAsync(
            List<SupplierResourceDto> batch);
        Task MarkSlaSignedAsync(Guid companyId);
        Task SetPasswordAsync(Guid companyId, string passwordHash);
        
        Task<IEnumerable<SupplierCapacity>> GetByStageAsync(
            Guid companyId,
            ApprovalStage stage,
            SupplierStatus? status
        );


        Task<SupplierCapacity> GetByIdAsync(Guid id);

        Task UpdateAsync(SupplierCapacity capacity);

        Task UpdateAsyncReferEmployee(SupplierCapacity entity);

        Task<IEnumerable<SupplierCapacity>> GetAllDataByStageAsync(
            ApprovalStage stage,
            SupplierStatus? status);
        Task UpdateCompanyAsync(Guid companyId, UpdateCompanyRequestDto dto);

    }
}
