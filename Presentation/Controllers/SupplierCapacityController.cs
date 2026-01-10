using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicFormPresentation.Controllers;

[ApiController]
[Route("api/supplier/capacities")]
public class SupplierCapacityController : ControllerBase
{
    private readonly ISupplierServiceInterface _service;

    public SupplierCapacityController(ISupplierServiceInterface service)
    {
        _service = service;
    }

    // 🔥 UPDATED: Added companyId
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid companyId,
        [FromQuery] string filter = "all")
    {
        return Ok(await _service.GetSupplierCapacitiesAsync(companyId, filter));
    }
    
    [HttpGet("getAllPendingCandidates")]
    public async Task<IActionResult> GetAllPendingCanidate()
    {
        return Ok(await _service.GetAllSupplierCapacitiesAsync("pending"));
        
    }

    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligibleSuppliers()
    {
        var result = await _service.GetEligibleSuppliersAsync();
        return Ok(result);
    }
    
    
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id,bool? isRequestAdmin)
    {
        await _service.SupplierApproveAsync(id, isRequestAdmin);
        return Ok();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] string remark, bool? isRequestAdmin)
    {
        await _service.SupplierRejectAsync(id, remark, isRequestAdmin);
        return Ok();
    }
    
    [HttpGet("admin/approved")]
    public async Task<IActionResult> GetApprovedByAdmin()
    {
        var result = await _service.GetAdminApprovedAsync();
        return Ok(result);
    }

    [HttpGet("admin/rejected")]
    public async Task<IActionResult> GetRejectedByAdmin()
    {
        var result = await _service.GetAdminRejectedAsync();
        return Ok(result);
    }

}
