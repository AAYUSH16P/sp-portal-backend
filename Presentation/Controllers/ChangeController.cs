using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Dtos;

namespace DynamicFormPresentation.Controllers;

public class ChangeController : Controller
{
    private readonly ICompanyChangeRequestService _service;
    public ChangeController(ICompanyChangeRequestService companyApproval)
    {
        _service =  companyApproval;
    }
    
    [HttpPost("admin/company-change-requests/{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        Guid adminId = Guid.Parse("25c37715-ad0f-4180-816c-c8b025000000");
        await _service.ApproveAsync(id, adminId);
        return Ok();
    }
    
    [HttpPost("admin/company-change-requests/{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] string remark)
    {
        Guid adminId = Guid.Parse("25c37715-ad0f-4180-816c-c8b025000000");
        await _service.RejectAsync(id, remark, adminId);
        return Ok();
    }

    
    [HttpGet("admin/company-change-requests")]
    public async Task<IActionResult> GetPending()
    {
        return Ok(await _service.GetPendingAsync());
    }

    
    [HttpPost("company/change-request")]
    public async Task<IActionResult> SubmitChangeRequest(
        [FromBody] CompanyChangeRequestDto dto)
    {
        await _service.SubmitAsync(dto);
        return Ok(new { message = "Change request submitted" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto dto)
    {
        await _service.ForgotPasswordAsync(dto.Email);
        return Ok(new { message = "If email exists, reset link sent" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto dto)
    {
        await _service.ResetPasswordAsync(dto.Token, dto.NewPassword);
        return Ok(new { message = "Password reset successful" });
    }

}