using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Services;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;

    public SuperAdminController(ISuperAdminService superAdminService)
    {
        _superAdminService = superAdminService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SuperAdminLoginDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "يرجى إدخال رمز التحقق الساعي للـ Super Admin" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _superAdminService.LoginAsync(dto, ipAddress, cancellationToken);
        if (result == null)
        {
            return Unauthorized(new { message = "رمز التحقق الساعي غير صحيح أو انتهت صلاحيته أو تم استخدامه مسبقاً. يرجى الحصول على الرمز الجديد." });
        }

        return Ok(result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("backlog")]
    [HttpGet("audit-backlogs")]
    public async Task<IActionResult> GetAuditBacklog(
        [FromQuery] AuditBacklogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _superAdminService.GetAuditBacklogAsync(query, cancellationToken);
        return Ok(result);
    }
}
