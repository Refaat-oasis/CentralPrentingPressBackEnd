using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CentralPress.API.Services;

public class JobTitleService : IJobTitleService
{
    private readonly ApplicationDbContext _context;

    public JobTitleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobTitleSummaryViewModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobTitles
            .OrderBy(j => j.JobTitleId)
            .Select(j => new JobTitleSummaryViewModel
            {
                JobTitleId = j.JobTitleId,
                TitleNameAr = j.TitleNameAr,
                Description = j.Description,
                IsActive = j.IsActive,
                EmployeesCount = j.Users.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<JobTitleSummaryViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var j = await _context.JobTitles
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.JobTitleId == id, cancellationToken);

        if (j == null) return null;

        return new JobTitleSummaryViewModel
        {
            JobTitleId = j.JobTitleId,
            TitleNameAr = j.TitleNameAr,
            Description = j.Description,
            IsActive = j.IsActive,
            EmployeesCount = j.Users.Count
        };
    }

    public async Task<(bool Succeeded, string Message, int? TitleId)> CreateAsync(JobTitleDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.TitleNameAr))
            return (false, "يرجى كتابة اسم المسمى الوظيفي", null);

        var title = new JobTitle
        {
            TitleNameAr = dto.TitleNameAr.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true
        };

        _context.JobTitles.Add(title);
        await _context.SaveChangesAsync(cancellationToken);

        return (true, "تمت إضافة المسمى الوظيفي بنجاح", title.JobTitleId);
    }

    public async Task<(bool Succeeded, string Message)> UpdateAsync(int id, JobTitleDto dto, CancellationToken cancellationToken = default)
    {
        var title = await _context.JobTitles.FindAsync([id], cancellationToken);
        if (title == null) return (false, "المسمى الوظيفي غير موجود");

        if (string.IsNullOrWhiteSpace(dto.TitleNameAr))
            return (false, "يرجى كتابة اسم المسمى الوظيفي");

        title.TitleNameAr = dto.TitleNameAr.Trim();
        title.Description = dto.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return (true, "تم تعديل المسمى الوظيفي بنجاح");
    }

    public async Task<(bool Succeeded, string Message, bool IsActive)> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        // Use IgnoreQueryFilters so we can find deactivated titles to reactivate
        var title = await _context.JobTitles.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.JobTitleId == id, cancellationToken);
        if (title == null) return (false, "المسمى الوظيفي غير موجود", false);

        title.IsActive = !title.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        string msg = title.IsActive ? "تم تفعيل المسمى الوظيفي بنجاح" : "تم تعطيل المسمى الوظيفي بنجاح";
        return (true, msg, title.IsActive);
    }

    public async Task<RolePermissionsMatrixViewModel?> GetRolePermissionsAsync(int id, CancellationToken cancellationToken = default)
    {
        var title = await _context.JobTitles.FindAsync([id], cancellationToken);
        if (title == null) return null;

        var modules = await _context.SystemModules.Where(m => m.IsActive).ToListAsync(cancellationToken);
        var existingPerms = await _context.RolePermissions.Where(r => r.JobTitleId == id).ToListAsync(cancellationToken);

        var list = new List<ModulePermissionViewModel>();
        foreach (var mod in modules)
        {
            var p = existingPerms.FirstOrDefault(r => r.ModuleId == mod.ModuleId);
            list.Add(new ModulePermissionViewModel
            {
                ModuleId = mod.ModuleId,
                ModuleKey = mod.ModuleKey,
                ModuleNameAr = mod.ModuleNameAr,
                CanAccess = p?.CanAccess ?? false,
                CanCreate = p?.CanCreate ?? false,
                CanUpdate = p?.CanUpdate ?? false,
                CanToggleActive = p?.CanToggleActive ?? false
            });
        }

        return new RolePermissionsMatrixViewModel
        {
            JobTitleId = title.JobTitleId,
            TitleNameAr = title.TitleNameAr,
            Permissions = list
        };
    }

    public async Task<(bool Succeeded, string Message)> SaveRolePermissionsAsync(int id, List<RolePermissionInputDto> permissions, CancellationToken cancellationToken = default)
    {
        var title = await _context.JobTitles.FindAsync([id], cancellationToken);
        if (title == null) return (false, "المسمى الوظيفي غير موجود");

        // HIGH-22: Transactional updates with provider safety check
        await using IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var existing = await _context.RolePermissions.Where(r => r.JobTitleId == id).ToListAsync(cancellationToken);
            _context.RolePermissions.RemoveRange(existing);

            if (permissions != null && permissions.Any())
            {
                var newPerms = permissions.Select(dto => new RolePermission
                {
                    JobTitleId = id,
                    ModuleId = dto.ModuleId,
                    CanAccess = dto.CanAccess,
                    CanCreate = dto.CanCreate,
                    CanUpdate = dto.CanUpdate,
                    CanToggleActive = dto.CanToggleActive
                });

                await _context.RolePermissions.AddRangeAsync(newPerms, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (true, "تم حفظ صلاحيات المسمى الوظيفي بنجاح");
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return (false, $"حدث خطأ أثناء حفظ الصلاحيات: {ex.Message}");
        }
    }

    public async Task<List<ModulePermissionViewModel>> GetSystemModulesAsync(CancellationToken cancellationToken = default)
    {
        var modules = await _context.SystemModules.Where(m => m.IsActive).ToListAsync(cancellationToken);
        return modules.Select(m => new ModulePermissionViewModel
        {
            ModuleId = m.ModuleId,
            ModuleKey = m.ModuleKey,
            ModuleNameAr = m.ModuleNameAr,
            CanAccess = true,
            CanCreate = true,
            CanUpdate = true,
            CanToggleActive = true
        }).ToList();
    }

    public async Task<(bool Succeeded, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var title = await _context.JobTitles.FindAsync([id], cancellationToken);
        if (title == null)
            return (false, "المسمى الوظيفي غير موجود");

        // MED-06: Prevent deletion of role/job title if users are assigned to it
        var assignedCount = await _context.Users.CountAsync(u => u.JobTitleId == id, cancellationToken);
        if (assignedCount > 0)
        {
            return (false, $"لا يمكن حذف المسمى الوظيفي لوجود ({assignedCount}) موظف مسند إليه حالياً. يرجى إعادة تعيين الموظفين أولاً.");
        }

        // HIGH-22: Transactional deletion with provider safety check
        await using IDbContextTransaction? transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var existingPermissions = await _context.RolePermissions.Where(r => r.JobTitleId == id).ToListAsync(cancellationToken);
            _context.RolePermissions.RemoveRange(existingPermissions);

            _context.JobTitles.Remove(title);
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (true, "تم حذف المسمى الوظيفي بنجاح");
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return (false, $"حدث خطأ أثناء حذف المسمى الوظيفي: {ex.Message}");
        }
    }
}
