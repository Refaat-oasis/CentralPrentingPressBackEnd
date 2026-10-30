using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/admin-users")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminUsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public record CreateEmployeeDto(
        string EmployeeCode,
        string FullName,
        string Email,
        string DummyPassword,
        int? JobTitleId,
        string? PhotoUrl
    );

    public record UpdateEmployeeDto(
        string EmployeeCode,
        string FullName,
        string Email,
        int? JobTitleId,
        string? PhotoUrl
    );

    public record PermissionOverrideInputDto(
        int ModuleId,
        bool? CanAccess,
        bool? CanCreate,
        bool? CanUpdate,
        bool? CanToggleActive
    );

    [HttpGet]
    [PermissionAuthorize("EMPLOYEES", "CanAccess")]
    public async Task<IActionResult> GetAllEmployees()
    {
        var users = await _context.Users
            .Include(u => u.JobTitle)
            .OrderBy(u => u.EmployeeCode)
            .Select(u => new
            {
                u.Id,
                u.EmployeeCode,
                u.FullName,
                u.Email,
                u.PhotoUrl,
                u.JobTitleId,
                JobTitleName = u.JobTitle != null ? u.JobTitle.TitleNameAr : "غير محدد",
                u.IsActive,
                u.MustChangePassword,
                u.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    [PermissionAuthorize("EMPLOYEES", "CanCreate")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeCode) || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "يرجى ملء كافة الحقول الأساسية (الكود، الاسم، البريد)" });

        if (await _context.Users.AnyAsync(u => u.EmployeeCode == dto.EmployeeCode))
            return BadRequest(new { message = "كود الموظف مستخدم بالفعل" });

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "البريد الإلكتروني مستخدم بالفعل" });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmployeeCode = dto.EmployeeCode.Trim(),
            FullName = dto.FullName.Trim(),
            JobTitleId = dto.JobTitleId,
            PhotoUrl = dto.PhotoUrl,
            MustChangePassword = true, // Forced change on first login
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var password = string.IsNullOrWhiteSpace(dto.DummyPassword) ? "Aup@12345" : dto.DummyPassword;
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = "تمت إضافة الموظف بنجاح بكلمة مرور مبدئية", userId = user.Id });
    }

    [HttpPut("{id}")]
    [PermissionAuthorize("EMPLOYEES", "CanUpdate")]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "الموظف غير موجود" });

        if (await _context.Users.AnyAsync(u => u.EmployeeCode == dto.EmployeeCode && u.Id != id))
            return BadRequest(new { message = "كود الموظف مستخدم بالفعل لموظف آخر" });

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            return BadRequest(new { message = "البريد الإلكتروني مستخدم بالفعل لموظف آخر" });

        user.EmployeeCode = dto.EmployeeCode.Trim();
        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim();
        user.UserName = dto.Email.Trim();
        user.JobTitleId = dto.JobTitleId;
        user.PhotoUrl = dto.PhotoUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = "تم تعديل بيانات الموظف بنجاح" });
    }

    [HttpPatch("{id}/toggle-active")]
    [PermissionAuthorize("EMPLOYEES", "CanToggleActive")]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "الموظف غير موجود" });

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = user.IsActive ? "تم تفعيل حساب الموظف" : "تم تعطيل حساب الموظف",
            isActive = user.IsActive
        });
    }

    [HttpGet("{id}/overrides")]
    [PermissionAuthorize("EMPLOYEES", "CanAccess")]
    public async Task<IActionResult> GetUserPermissionOverrides(string id)
    {
        var overrides = await _context.UserPermissionOverrides
            .Where(o => o.UserId == id)
            .ToListAsync();

        return Ok(overrides);
    }

    [HttpPost("{id}/overrides")]
    [PermissionAuthorize("EMPLOYEES", "CanUpdate")]
    public async Task<IActionResult> SaveUserPermissionOverrides(string id, [FromBody] List<PermissionOverrideInputDto> overrides)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "الموظف غير موجود" });

        var existing = await _context.UserPermissionOverrides.Where(o => o.UserId == id).ToListAsync();
        _context.UserPermissionOverrides.RemoveRange(existing);

        foreach (var item in overrides)
        {
            _context.UserPermissionOverrides.Add(new UserPermissionOverride
            {
                UserId = id,
                ModuleId = item.ModuleId,
                CanAccess = item.CanAccess,
                CanCreate = item.CanCreate,
                CanUpdate = item.CanUpdate,
                CanToggleActive = item.CanToggleActive
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم تحديث الصلاحيات المخصصة للموظف بنجاح" });
    }
}
