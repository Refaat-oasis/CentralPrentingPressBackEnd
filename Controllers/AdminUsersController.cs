using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/admin-users")]
[Route("api/adminusers")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IEmailService _emailService;

    public AdminUsersController(IAdminUserService adminUserService, IEmailService emailService)
    {
        _adminUserService = adminUserService;
        _emailService = emailService;
    }

    [HttpGet]
    [PermissionAuthorize("EMPLOYEES", "CanAccess")]
    public async Task<IActionResult> GetAllEmployees(CancellationToken cancellationToken)
    {
        var users = await _adminUserService.GetAllEmployeesAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id}")]
    [PermissionAuthorize("EMPLOYEES", "CanAccess")]
    public async Task<IActionResult> GetEmployeeById(string id, CancellationToken cancellationToken)
    {
        var user = await _adminUserService.GetEmployeeByIdAsync(id, cancellationToken);
        if (user == null) return NotFound(new { message = "الموظف غير موجود" });
        return Ok(user);
    }

    [HttpPost]
    [PermissionAuthorize("EMPLOYEES", "CanCreate")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message, employee) = await _adminUserService.CreateEmployeeAsync(dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, employee });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("EMPLOYEES", "CanUpdate")]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _adminUserService.UpdateEmployeeAsync(id, dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPatch("{id}/toggle-active")]
    [HttpPut("{id}/toggle-status")]
    [PermissionAuthorize("EMPLOYEES", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(string id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (succeeded, message, isActive) = await _adminUserService.ToggleActiveAsync(id, currentUserId, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, isActive });
    }

    [HttpGet("{id}/overrides")]
    [PermissionAuthorize("EMPLOYEES", "CanAccess")]
    public async Task<IActionResult> GetUserOverrides(string id, CancellationToken cancellationToken)
    {
        var overrides = await _adminUserService.GetUserOverridesAsync(id, cancellationToken);
        return Ok(overrides);
    }

    [HttpPost("{id}/overrides")]
    [HttpPut("{id}/overrides")]
    [PermissionAuthorize("EMPLOYEES", "CanUpdate")]
    public async Task<IActionResult> SaveUserOverrides(string id, [FromBody] SaveOverridesDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _adminUserService.SaveUserOverridesAsync(id, dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpDelete("{id}")]
    [PermissionAuthorize("EMPLOYEES", "CanUpdate")]
    public async Task<IActionResult> DeleteEmployee(string id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (succeeded, message) = await _adminUserService.DeleteEmployeeAsync(id, currentUserId, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("test-smtp")]
    [Authorize(Roles = "Developer,Admin")]
    [PermissionAuthorize("EMPLOYEES", "CanAccess")]
    public async Task<IActionResult> TestSmtp([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TestSmtpRequestDto? dto = null)
    {
        var recipient = !string.IsNullOrWhiteSpace(dto?.RecipientEmail)
            ? dto.RecipientEmail.Trim()
            : User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(recipient))
            return BadRequest(new { message = "يرجى تحديد بريد إلكتروني صالح لإرسال اختبار SMTP" });

        var (success, message, _) = await _emailService.TestSmtpConnectionAsync(recipient);
        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("{id}/resend-invitation")]
    [PermissionAuthorize("EMPLOYEES", "CanUpdate")]
    public async Task<IActionResult> ResendInvitation(string id, CancellationToken cancellationToken)
    {
        var (succeeded, message, emailSent) = await _adminUserService.ResendInvitationAsync(id, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { succeeded = true, message, emailSent });
    }
}
