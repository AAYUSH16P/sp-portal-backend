using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Shared;

namespace DynamicFormPresentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierServiceInterface _supplierServiceInterface;
        private readonly IBackgroundJobClient _backgroundJobClient;


        public SupplierController(ISupplierServiceInterface supplierServiceInterface,IBackgroundJobClient backgroundJobClient)
        {
            _supplierServiceInterface = supplierServiceInterface;
            _backgroundJobClient = backgroundJobClient;
        }
        
        [HttpPost("{companyId}/bulk-upload")]
        public async Task<IActionResult> ResourceBulkUpload(
            Guid companyId,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var allowedExtensions = new[] { ".xls", ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Only Excel files (.xls, .xlsx) are allowed");

            try
            {
                using var stream = file.OpenReadStream();

                // ✅ Validate template headers
                ExcelTemplateValidator.Validate(stream);
                stream.Position = 0;

                // ✅ Save upload metadata
                var uploadId = await _supplierServiceInterface.SheetUploadAsync(
                    stream,
                    file.FileName,
                    file.Length,
                    1,
                    companyId
                );

                // ✅ Enqueue background job ONLY after validation passes
                _backgroundJobClient.Enqueue<ISupplierServiceInterface>(
                    s => s.ProcessUploadAsync(uploadId, companyId)
                );

                return Ok(new
                {
                    UploadId = uploadId,
                    Message = "Excel upload accepted and queued for processing"
                });
            }
            catch (TemplateValidationException ex)
            {
                // ❗ Template-specific error
                return UnprocessableEntity(new
                {
                    Message = "Invalid Excel template",
                    Details = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Bulk upload failed due to server error"
                });
            }
        }


        [HttpPost("manual-upload")]
        public async Task<IActionResult> CreateSupplierCapacity(
            [FromBody] SupplierResourceDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid payload");

            await _supplierServiceInterface.CreateSupplierResourceAsync(dto);

            return Ok(new
            {
                message = "Supplier resource created successfully"
            });
        }
        
        
        [HttpPost("new-supplier-registered")]
        public async Task<IActionResult> NewSupplierRegistered(
            [FromBody] CompanyRegistrationRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid payload");

            try
            {
                await _supplierServiceInterface.SubmitCompanyAsync(dto);

                return Ok(new
                {
                    message = "Supplier resource created successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }
        
        [HttpPost("{companyId}/sign-sla")]
        public async Task<IActionResult> SignSla(Guid companyId)
        {
            await _supplierServiceInterface.SignSlaAsync(companyId);

            return Ok(new
            {
                Message = "SLA signed successfully",
                CompanyId = companyId
            });
        }
        
        [HttpPost("{companyId}/set-password")]
        public async Task<IActionResult> SetPassword(
            Guid companyId,
            [FromBody] SetPasswordDto dto)
        {
            await _supplierServiceInterface.SetCompanyPasswordAsync(companyId,dto.CurrentPassword, dto.Password);

            return Ok(new { Message = "Password set successfully" });
        }
        
        
        [HttpPut("manual-upload/{id}")]
        public async Task<IActionResult> UpdateSupplierCapacity(
            Guid id,
            [FromBody] SupplierResourceDto dto)
        {
            if (dto == null || id == Guid.Empty)
                return BadRequest("Invalid payload");

            dto.Id = id;

            await _supplierServiceInterface.UpdateSupplierResourceAsync(dto);

            return Ok(new
            {
                message = "Supplier resource updated successfully"
            });
        }
        
        
        [HttpPut("{companyId}")]
        public async Task<IActionResult> UpdateCompany(
            Guid companyId,
            [FromBody] UpdateCompanyRequestDto dto)
        {
            await _supplierServiceInterface.UpdateCompanyAsync(companyId, dto);
            return Ok(new { message = "Company updated successfully" });
        }

        
        
        [HttpGet("All-supplier")]
        public async Task<IActionResult> GetAllSuppliers()
        {
            var ab = await _supplierServiceInterface.GetCompaniesLookupAsync();
            return Ok(ab);
        }


        
    }
}