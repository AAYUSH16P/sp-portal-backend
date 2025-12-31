using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicFormPresentation.Controllers;

[ApiController]
[Route("api/hr/capacities")]
public class HrCapacityController : ControllerBase
{
    private readonly ISupplierServiceInterface _service;

    public HrCapacityController(ISupplierServiceInterface service)
    {
        _service = service;
    }

    // 🔥 UPDATED: Added companyId
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid companyId,
        [FromQuery] string filter = "all")
    {
        return Ok(await _service.GetHrCapacitiesAsync(companyId, filter));
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _service.HrApproveAsync(id);
        return Ok();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] string remark)
    {
        await _service.HrRejectAsync(id, remark);
        return Ok();
    }
}
