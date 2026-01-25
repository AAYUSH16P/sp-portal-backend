using DynamicFormRepo.DynamicFormRepoInterface;
using DynamicFormService.DynamicFormServiceInterface;
using Shared;
using ClosedXML.Excel;
using FinancialManagementDataAccess.Models;
using Infrastructure.Security;
using Shared.Dtos;
using Shared.Enum;


namespace DynamicFormService.DynamicFormServiceImplementation
{
    public class SupplierServiceImplementation : ISupplierServiceInterface
    {
        private readonly ISupplierRepoInterface _supplierRepoInterface;
       
        public SupplierServiceImplementation(ISupplierRepoInterface supplierRepoInterface) 
        {
            _supplierRepoInterface = supplierRepoInterface;
        }
        
        private SupplierStatus? ParseFilter(string filter)
        {
            return filter?.ToLower() switch
            {
                "approved" => SupplierStatus.Approved,
                "rejected" => SupplierStatus.Rejected,
                "pending" => SupplierStatus.Pending,
                _ => null
            };
        }
       
        public async Task<Guid> SubmitCompanyAsync(CompanyRegistrationRequestDto request)
        {
            return await _supplierRepoInterface.SubmitCompanyAsync(request);
        }
        public async Task<long> SheetUploadAsync(
            Stream fileStream,
            string fileName,
            long fileSize,
            int companyId,
            Guid uploadedBy)
        {
            var uploadId =
                await _supplierRepoInterface.CreateUploadAsync(
                    uploadedBy,
                    companyId,
                    fileName
                );

            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);

            await _supplierRepoInterface.SaveFileAsync(uploadId, ms.ToArray(), fileSize);

            return uploadId;
        }


        public async Task ProcessUploadAsync(long uploadId, Guid companyId)
        {
            var bytes = await _supplierRepoInterface.GetFileAsync(uploadId);

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var batch = new List<SupplierResourceDto>();

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var totalExp = row.Cell(8).GetValue<decimal>();
                var workingSinceCell = row.Cell(2);
                DateTime workingSince;

               

                var dto = new SupplierResourceDto
                {
                    CompanyId = companyId,     // 🔥 FIXED
                    WorkingSince = ReadExcelDate(row.Cell(2), "WorkingSince"),

                    CompanyEmployeeId = row.Cell(1).GetString(),
                    CTC = row.Cell(3).GetValue<decimal>(),
                    JobTitle = row.Cell(4).GetString(),
                    Role = row.Cell(5).GetString(),
                    Gender = row.Cell(6).GetString(),
                    Location = row.Cell(7).GetString(),
                    TotalExperience = totalExp,
                    TechnicalSkills = row.Cell(9).GetString(),
                    Tools = row.Cell(10).GetString(),
                    NumberOfProjects = row.Cell(11).GetValue<int>(),
                    EmployerNote = row.Cell(12).GetString(),
                    Certifications = row.Cell(13).GetString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList(),

                    IsRefered = false,
                    ApprovalStage = ApprovalStage.Supplier,
                    Status = totalExp >= 1
                        ? SupplierStatus.Approved
                        : SupplierStatus.Pending
                };

                ValidateRow(dto);
                batch.Add(dto);
            }

            await _supplierRepoInterface.InsertSupplierResourcesBatchAsync(batch);
        }

        public async Task CreateSupplierResourceAsync( SupplierResourceDto dto)
        {
            ValidateRow(dto);

            // 🔒 FORCE company ownership
            

            // ================================
            // APPROVAL RULES
            // ================================
            dto.ApprovalStage = dto.IsRefered
                ? ApprovalStage.HR          // Option 3
                : ApprovalStage.Supplier;   // Option 1

            
            if (!dto.IsRefered &&
                dto.WorkingSince <= DateOnly.FromDateTime(DateTime.Today.AddYears(-1)))
            {
                dto.Status = SupplierStatus.Approved;
            }
            else
            {
                dto.Status = SupplierStatus.Pending;
            }

            await _supplierRepoInterface.InsertSupplierResourcesBatchAsync(
                new List<SupplierResourceDto> { dto }
            );
        }

        
        public async Task SignSlaAsync(Guid companyId)
        {
            await _supplierRepoInterface.MarkSlaSignedAsync(companyId);
        }
        
        
        public async Task SetCompanyPasswordAsync(Guid companyId,string currentPassword, string newPassword)
        {
            if (newPassword.Length < 8)
                throw new Exception("Password must be at least 8 characters");

            var existingHash = await _supplierRepoInterface.GetPasswordHashAsync(companyId);

            if (string.IsNullOrEmpty(existingHash))
                throw new Exception("Password not set");

            // 🔐 Verify current password
            var isValid = PasswordHasher.Verify(currentPassword, existingHash);

            if (!isValid)
                throw new Exception("Current password is incorrect");

            // 🔐 Hash new password
            var newHash = PasswordHasher.Hash(newPassword);

            await _supplierRepoInterface.SetPasswordAsync(companyId, newHash);
        }
        
        
        public async Task<IEnumerable<SupplierResourceDto>> GetHrCapacitiesAsync(
            Guid companyId,
            string filter)
        {
            var data = await _supplierRepoInterface.GetByStageAsync(
                companyId,
                ApprovalStage.HR,
                ParseFilter(filter)
            );

            return Map(data);
        }

        public async Task<IEnumerable<SupplierResourceDto>> GetSupplierCapacitiesAsync(
            Guid companyId,
            string filter)
        {
            var data = await _supplierRepoInterface.GetByStageAsyncBySupplier(
                companyId,
                ApprovalStage.Supplier,
                ParseFilter(filter)
            );

            return Map(data);
        }
        
        
        
        public async  Task<IEnumerable<SupplierResourceDto>> GetAllSupplierCapacitiesAsync(string filter)
        {
            var data = await _supplierRepoInterface.GetAllDataByStageAsync(ApprovalStage.Supplier, ParseFilter(filter));

            return Map(data);
        }

        
        


    public async Task HrApproveAsync(Guid id)
    {
        var c = await _supplierRepoInterface.GetByIdAsync(id);
        c.Status = SupplierStatus.Pending;
        c.ApprovalStage = ApprovalStage.Supplier;
        await _supplierRepoInterface.UpdateAsync(c);
    }

    public async Task HrRejectAsync(Guid id, string remark)
    {
        var c = await _supplierRepoInterface.GetByIdAsync(id);
        c.Status = SupplierStatus.Rejected;
        c.ApprovalStage = ApprovalStage.HR;
        c.Remark = remark;
        await _supplierRepoInterface.UpdateAsync(c);
    }

    public async Task SupplierApproveAsync(Guid id,bool? isRequestAdmin)
    {
        var c = await _supplierRepoInterface.GetByIdAsync(id);

        c.Status = SupplierStatus.Approved;

        c.ApprovalStage = ApprovalStage.Completed;
        if (isRequestAdmin.HasValue)
        {
            c.AdminDecision = isRequestAdmin; 
        }
        await _supplierRepoInterface.UpdateAsync(c);
    }

    public async Task SupplierRejectAsync(Guid id, string remark, bool? isRequestAdmin)
    {
        var c = await _supplierRepoInterface.GetByIdAsync(id);
        c.Status = SupplierStatus.Rejected;
        c.ApprovalStage = ApprovalStage.Supplier;
        c.Remark = remark;
        c.AdminDecision = isRequestAdmin;
        await _supplierRepoInterface.UpdateAsync(c);
    }
    
    public async Task UpdateSupplierResourceAsync(SupplierResourceDto dto)
    {
        if (dto.Id == null)
            throw new Exception("Invalid resource id");

        var entity = await _supplierRepoInterface.GetByIdAsync(dto.Id.Value);
        if (entity == null)
            throw new Exception("Resource not found");

        // 🔹 Scalar fields
        entity.CompanyEmployeeId = dto.CompanyEmployeeId;
        entity.WorkingSince = dto.WorkingSince; // ✅ DateOnly stays DateOnly
        entity.CTC = dto.CTC;
        entity.JobTitle = dto.JobTitle;
        entity.Role = dto.Role;
        entity.Gender = dto.Gender;
        entity.Location = dto.Location;
        entity.TotalExperience = dto.TotalExperience;
        entity.TechnicalSkills = dto.TechnicalSkills;
        entity.Tools = dto.Tools;
        entity.NumberOfProjects = dto.NumberOfProjects;
        entity.EmployerNote = dto.EmployerNote;

        // 🔹 Certifications mapping (CORRECT)
        entity.Certifications = dto.Certifications?
                                    .Where(c => !string.IsNullOrWhiteSpace(c))
                                    .Select(c => new SupplierCertification
                                    {
                                        Id = Guid.NewGuid(),
                                        SupplierCapacityId = entity.Id,
                                        CertificationName = c.Trim()
                                    })
                                    .ToList()
                                ?? new List<SupplierCertification>();

        // 🔹 Persist
        await _supplierRepoInterface.UpdateAsyncReferEmployee(entity);
    }
    
    public Task<IEnumerable<CompanyListDto>> GetCompaniesLookupAsync()
        => _supplierRepoInterface.GetCompaniesLookupAsync();


    private IEnumerable<SupplierResourceDto> Map(IEnumerable<SupplierCapacity> list)
    {
        return list.Select(x => new SupplierResourceDto
        {
            Id = x.Id,
            CompanyEmployeeId = x.CompanyEmployeeId,

            // ✅ Company details
            CompanyId = x.CompanyId,
            CompanyName = x.CompanyName,

            // ✅ Core details
            IsRefered = x.IsRefered,
            WorkingSince = x.WorkingSince,
            CTC = x.CTC,
            JobTitle = x.JobTitle,
            Role = x.Role,
            Gender = x.Gender,
            Location = x.Location,
            TotalExperience = x.TotalExperience,

            TechnicalSkills = x.TechnicalSkills,
            Tools = x.Tools,
            NumberOfProjects = x.NumberOfProjects,

            // ✅ Status & approval
            Status = x.Status,
            ApprovalStage = x.ApprovalStage,
            EmployerNote = x.EmployerNote,

            // ✅ THIS WAS MISSING
            CreatedAt = x.CreatedAt,

            // ✅ Certifications
            Certifications = x.Certifications?
                                 .Where(c => c != null && !string.IsNullOrWhiteSpace(c.CertificationName))
                                 .Select(c => c.CertificationName)
                                 .ToList()
                             ?? new List<string>()
        });
    }

       
        private void ValidateRow(SupplierResourceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyEmployeeId))
                throw new Exception("CompanyEmployeeId required");

            if (dto.CTC <= 0)
                throw new Exception("Invalid CTC");

            if (dto.TotalExperience < 0)
                throw new Exception("Invalid experience");

            if (dto.NumberOfProjects < 0)
                throw new Exception("Invalid project count");
        }
        
        
        private static DateOnly ReadExcelDate(IXLCell cell, string columnName)
        {
            if (cell == null || cell.IsEmpty())
                throw new Exception($"{columnName} is required");

            // Case 1: True Excel date
            if (cell.DataType == XLDataType.DateTime)
            {
                return DateOnly.FromDateTime(cell.GetDateTime());
            }

            // Case 2: Excel numeric date (OADate)
            if (cell.DataType == XLDataType.Number)
            {
                return DateOnly.FromDateTime(DateTime.FromOADate(cell.GetDouble()));
            }

            // Case 3: Text date
            var text = cell.GetString().Trim();
            if (DateTime.TryParse(text, out var parsed))
            {
                return DateOnly.FromDateTime(parsed);
            }

            throw new Exception($"Invalid date in column {columnName}: {cell.GetString()}");
        }
    
        
        public async Task UpdateCompanyAsync(Guid companyId, UpdateCompanyRequestDto dto)
        {
            if (dto.YearEstablished < 1900)
                throw new Exception("Invalid year established");

            await _supplierRepoInterface.UpdateCompanyAsync(companyId, dto);
        }
        
        public Task<IEnumerable<SupplierResourceDto>> GetEligibleSuppliersAsync()
            => _supplierRepoInterface.GetEligibleSuppliersAsync();
        
        public async Task<IEnumerable<SupplierAdminCapacityDto>> GetAdminApprovedAsync()
        {
            return await _supplierRepoInterface.GetApprovedByAdminAsync();
        }

        public async Task<IEnumerable<SupplierAdminCapacityDto>> GetAdminRejectedAsync()
        {
            return await _supplierRepoInterface.GetRejectedByAdminAsync();
        }


    }
}