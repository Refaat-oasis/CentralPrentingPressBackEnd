using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/job-titles")]
[Authorize]
public class JobTitlesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public JobTitlesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public record JobTitleDto(string TitleNameAr, string? Description);
    public record RolePermissionInputDto(int ModuleId, bool CanAccess, bool CanCreate, bool CanUpdate, bool CanToggleActive);

    [HttpGet]
    [PermissionAuthorize("JOB_TITLES", "CanAccess")]
    public async Task<IActionResult> GetAll()
    {
        var titles = await _context.JobTitles
            .OrderBy(j => j.JobTitleId)
            .Select(j => new
            {
                j.JobTitleId,
                j.TitleNameAr,
                j.Description,
                j.IsActive,
                EmployeesCount = j.Users.Count
            })
            .ToListAsync();

        return Ok(titles);
    }

    [HttpPost]
    [PermissionAuthorize("JOB_TITLES", "CanCreate")]
    public async Task<IActionResult> Create([FromBody] JobTitleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TitleNameAr))
            return BadRequest(new { message = "يرجى كتابة اسم المسمى الوظيفي" });

        var title = new JobTitle
        {
            TitleNameAr = dto.TitleNameAr.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true
        };

        _context.JobTitles.Add(title);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تمت إضافة المسمى الوظيفي بنجاح", titleId = title.JobTitleId });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("JOB_TITLES", "CanUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] JobTitleDto dto)
    {
        var title = await _context.JobTitles.FindAsync(id);
        if (title == null) return NotFound(new { message = "المسمى الوظيفي غير موجود" });

        title.TitleNameAr = dto.TitleNameAr.Trim();
        title.Description = dto.Description?.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تعديل المسمى الوظيفي بنجاح" });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("JOB_TITLES", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var title = await _context.JobTitles.FindAsync(id);
        if (title == null) return NotFound(new { message = "المسمى الوظيفي غير موجود" });

        title.IsActive = !title.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { message = title.IsActive ? "تم تفعيل المسمى الوظيفي" : "تم تعطيل المسمى الوظيفي", isActive = title.IsActive });
    }

    [HttpGet("{id}/permissions")]
    [PermissionAuthorize("JOB_TITLES", "CanAccess")]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var title = await _context.JobTitles.FindAsync(id);
        if (title == null) return NotFound(new { message = "المسمى الوظيفي غير موجود" });

        var modules = await _context.SystemModules.Where(m => m.IsActive).ToListAsync();
        var existingPermissions = await _context.RolePermissions.Where(r => r.JobTitleId == id).ToListAsync();

        var matrix = modules.Select(m =>
        {
            var p = existingPermissions.FirstOrDefault(ep => ep.ModuleId == m.ModuleId);
            return new
            {
                m.ModuleId,
                m.ModuleKey,
                m.ModuleNameAr,
                CanAccess = p?.CanAccess ?? false,
                CanCreate = p?.CanCreate ?? false,
                CanUpdate = p?.CanUpdate ?? false,
                CanToggleActive = p?.CanToggleActive ?? false
            };
        }).ToList();

        return Ok(new
        {
            jobTitle = new { title.JobTitleId, title.TitleNameAr },
            permissions = matrix
        });
    }

    [HttpPost("{id}/permissions")]
    [PermissionAuthorize("JOB_TITLES", "CanUpdate")]
    public async Task<IActionResult> SaveRolePermissions(int id, [FromBody] List<RolePermissionInputDto> permissions)
    {
        var title = await _context.JobTitles.FindAsync(id);
        if (title == null) return NotFound(new { message = "المسمى الوظيفي غير موجود" });

        var existing = await _context.RolePermissions.Where(r => r.JobTitleId == id).ToListAsync();
        _context.RolePermissions.RemoveRange(existing);

        foreach (var p in permissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                JobTitleId = id,
                ModuleId = p.ModuleId,
                CanAccess = p.CanAccess,
                CanCreate = p.CanCreate,
                CanUpdate = p.CanUpdate,
                CanToggleActive = p.CanToggleActive
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم حفظ مصفوفة الصلاحيات بنجاح" });
    }
}
