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
    
    [HttpGet]
    public async Task<IActionResult> GetAllPendingCanidate()
    {
        return Ok(await _service.GetAllSupplierCapacitiesAsync("pending"));
        
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _service.SupplierApproveAsync(id);
        return Ok();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] string remark)
    {
        await _service.SupplierRejectAsync(id, remark);
        return Ok();
    }
}
