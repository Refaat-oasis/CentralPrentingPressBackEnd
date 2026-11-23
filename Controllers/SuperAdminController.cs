using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/developer")]
[Authorize(Roles = "Developer")]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;

    public SuperAdminController(ISuperAdminService superAdminService)
    {
        _superAdminService = superAdminService;
    }

    [HttpGet("backlog")]
    [HttpGet("audit-backlogs")]
    public async Task<IActionResult> GetAuditBacklog(
        [FromQuery] AuditBacklogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _superAdminService.GetAuditBacklogAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("hangfire-jobs")]
    public IActionResult GetHangfireJobs([FromQuery] string? state = null, [FromQuery] int limit = 50)
    {
        var overview = _superAdminService.GetHangfireOverview(state, limit);
        return Ok(overview);
    }

    [HttpPost("hangfire-jobs/{jobId}/requeue")]
    public IActionResult RequeueHangfireJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return BadRequest(new { message = "معرف المهمة غير صالح" });

        bool requeued = _superAdminService.RequeueHangfireJob(jobId);
        if (!requeued)
            return NotFound(new { message = "تعذر إعادة جدولة المهمة أو أنها غير موجودة" });

        return Ok(new { message = "تمت إعادة إرسال المهمة إلى الطابور للتنفيذ الفوري", jobId });
    }

    [HttpDelete("hangfire-jobs/{jobId}")]
    public IActionResult DeleteHangfireJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return BadRequest(new { message = "معرف المهمة غير صالح" });

        bool deleted = _superAdminService.DeleteHangfireJob(jobId);
        if (!deleted)
            return NotFound(new { message = "تعذر حذف المهمة أو أنها غير موجودة" });

        return Ok(new { message = "تم حذف المهمة من Hangfire بنجاح", jobId });
    }
}
