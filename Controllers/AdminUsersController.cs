using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/admin-users")]
[Route("api/adminusers")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
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
}
