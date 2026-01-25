
using FinancialManagementDataAccess.Models;
using Shared;
using Shared.Dtos;

namespace DynamicFormService.DynamicFormServiceInterface
{
    public interface ISupplierServiceInterface
    {
        Task<IEnumerable<SupplierResourceDto>> GetEligibleSuppliersAsync();

        Task UpdateSupplierResourceAsync(SupplierResourceDto dto);

        Task<long> SheetUploadAsync(
            Stream fileStream,
            string fileName,
            long fileSize,
            int companyId,
            Guid uploadedBy);        
        Task ProcessUploadAsync(long uploadId, Guid companyId);

        Task CreateSupplierResourceAsync( SupplierResourceDto dto);

        Task<Guid> SubmitCompanyAsync(CompanyRegistrationRequestDto request);
        Task SignSlaAsync(Guid companyId);

        Task SetCompanyPasswordAsync(Guid companyId, string currentPassword, string newPassword);
        
        Task<IEnumerable<SupplierResourceDto>> GetHrCapacitiesAsync(
            Guid companyId,
            string filter
        );

        Task<IEnumerable<SupplierResourceDto>> GetSupplierCapacitiesAsync(
            Guid companyId,
            string filter
        );

        Task HrApproveAsync(Guid id);
        Task HrRejectAsync(Guid id, string remark);

        Task SupplierApproveAsync(Guid id,bool? isRequestAdmin);
        Task SupplierRejectAsync(Guid id, string remark, bool? isRequestAdmin);

        Task<IEnumerable<SupplierResourceDto>> GetAllSupplierCapacitiesAsync(string filter);
        
        Task UpdateCompanyAsync(Guid companyId, UpdateCompanyRequestDto dto);
        
        Task<IEnumerable<SupplierAdminCapacityDto>> GetAdminApprovedAsync();
        Task<IEnumerable<SupplierAdminCapacityDto>> GetAdminRejectedAsync();
        Task<IEnumerable<CompanyListDto>> GetCompaniesLookupAsync();


    }
}
