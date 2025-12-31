using Microsoft.AspNetCore.Mvc;
using DynamicFormService.DynamicFormServiceInterface;
using Shared.Dtos;

namespace DynamicFormPresentation.Controllers;

[ApiController]
[Route("api/company")]
public class CompanyApprovalController : ControllerBase
{
    private readonly ICompanyApprovalService _service;

    public CompanyApprovalController(ICompanyApprovalService service)
    {
        _service = service;
    }
    
    [HttpGet("admin/pending-companies")]
    public async Task<IActionResult> GetPendingCompanies()
    {
        var result = await _service.GetPendingCompaniesAsync();
        return Ok(result);
    }

    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] CompanyLoginDto dto)
    {
        var result = await _service.LoginAsync(dto);
        return Ok(result);
    }

    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] ApproveCompanyDto dto)
    {
        await _service.ApproveAsync(dto.CompanyId);
        return Ok(new { message = "Company approved successfully" });
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] RejectCompanyDto dto)
    {
        await _service.RejectAsync(dto.CompanyId, dto.Remark);
        return Ok(new { message = "Company rejected successfully" });
    }
    
    
    [HttpGet("details")]
    public async Task<IActionResult> Details(Guid companyId)
    {
        var result = await _service.GetDetailsAsync(companyId);
        return Ok(result);
    }
}