using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/job-titles")]
[Route("api/jobtitles")]
[Authorize]
public class JobTitlesController : ControllerBase
{
    private readonly IJobTitleService _jobTitleService;

    public JobTitlesController(IJobTitleService jobTitleService)
    {
        _jobTitleService = jobTitleService;
    }

    [HttpGet]
    [PermissionAuthorize("JOB_TITLES", "CanAccess")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var titles = await _jobTitleService.GetAllAsync(cancellationToken);
        return Ok(titles);
    }

    [HttpGet("{id:int}")]
    [PermissionAuthorize("JOB_TITLES", "CanAccess")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var title = await _jobTitleService.GetByIdAsync(id, cancellationToken);
        if (title == null) return NotFound(new { message = "المسمى الوظيفي غير موجود" });
        return Ok(title);
    }

    [HttpPost]
    [PermissionAuthorize("JOB_TITLES", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] JobTitleDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message, titleId) = await _jobTitleService.CreateAsync(dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, titleId });
    }

    [HttpPut("{id:int}")]
    [PermissionAuthorize("JOB_TITLES", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] JobTitleDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _jobTitleService.UpdateAsync(id, dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPatch("{id:int}/toggle-active")]
    [PermissionAuthorize("JOB_TITLES", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        var (succeeded, message, isActive) = await _jobTitleService.ToggleActiveAsync(id, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, isActive });
    }

    [HttpGet("{id:int}/permissions")]
    [PermissionAuthorize("JOB_TITLES", "CanAccess")]
    public async Task<IActionResult> GetRolePermissions(int id, CancellationToken cancellationToken)
    {
        var matrix = await _jobTitleService.GetRolePermissionsAsync(id, cancellationToken);
        if (matrix == null) return NotFound(new { message = "المسمى الوظيفي غير موجود" });
        return Ok(matrix);
    }

    [HttpPost("{id:int}/permissions")]
    [PermissionAuthorize("JOB_TITLES", "CanUpdate")]
    public async Task<IActionResult> SaveRolePermissions(int id, [FromBody] List<RolePermissionInputDto> permissions, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _jobTitleService.SaveRolePermissionsAsync(id, permissions, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // HIGH-08: Dedicated system modules endpoint
    [HttpGet("modules")]
    [Authorize]
    public async Task<IActionResult> GetSystemModules(CancellationToken cancellationToken)
    {
        var modules = await _jobTitleService.GetSystemModulesAsync(cancellationToken);
        return Ok(new { permissions = modules });
    }

    // MED-06: Safe role deletion preventing orphaned employees
    [HttpDelete("{id:int}")]
    [PermissionAuthorize("JOB_TITLES", "CanUpdate")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _jobTitleService.DeleteAsync(id, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new OperationStatusViewModel { Success = true, Message = message });
    }
}
