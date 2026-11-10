using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class PermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> HasPermissionAsync(string userId, string moduleKey, string actionType)
    {
        var user = await _context.Users
            .Include(u => u.PermissionOverrides)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive) return false;

        // SuperAdmin or Admin role has full access to all modules
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return true;
        }

        var targetKey = (moduleKey ?? "").ToUpper();
        var module = await _context.SystemModules.FirstOrDefaultAsync(m =>
            m.ModuleKey.ToUpper() == targetKey && m.IsActive);
        if (module == null) return false;

        // 1. Check User Override
        var userOverride = user.PermissionOverrides.FirstOrDefault(o => o.ModuleId == module.ModuleId);
        if (userOverride != null)
        {
            bool? overrideVal = actionType.ToLowerInvariant() switch
            {
                "canaccess" or "access" => userOverride.CanAccess,
                "cancreate" or "create" => userOverride.CanCreate,
                "canupdate" or "update" => userOverride.CanUpdate,
                "cantoggleactive" or "toggleactive" => userOverride.CanToggleActive,
                _ => null
            };

            if (overrideVal.HasValue) return overrideVal.Value;
        }

        // 2. Check Job Title Role Permission
        if (user.JobTitleId.HasValue)
        {
            var rolePerm = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.JobTitleId == user.JobTitleId.Value && rp.ModuleId == module.ModuleId);

            if (rolePerm != null)
            {
                return actionType.ToLowerInvariant() switch
                {
                    "canaccess" or "access" => rolePerm.CanAccess,
                    "cancreate" or "create" => rolePerm.CanCreate,
                    "canupdate" or "update" => rolePerm.CanUpdate,
                    "cantoggleactive" or "toggleactive" => rolePerm.CanToggleActive,
                    _ => false
                };
            }
        }

        return false;
    }

    public async Task<List<ModuleEffectivePermissionViewModel>> GetUserPermissionsAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.PermissionOverrides)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var modules = await _context.SystemModules.Where(m => m.IsActive).ToListAsync();
        if (user == null) return new List<ModuleEffectivePermissionViewModel>();

        // SuperAdmin or Admin has full access to all modules
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return modules.Select(mod => new ModuleEffectivePermissionViewModel(
                mod.ModuleKey,
                mod.ModuleNameAr,
                true,
                true,
                true,
                true
            )).ToList();
        }

        var rolePermissions = user.JobTitleId.HasValue
            ? await _context.RolePermissions.Where(rp => rp.JobTitleId == user.JobTitleId.Value).ToListAsync()
            : new List<Entities.RolePermission>();

        var result = new List<ModuleEffectivePermissionViewModel>();

        foreach (var mod in modules)
        {
            var uo = user.PermissionOverrides.FirstOrDefault(o => o.ModuleId == mod.ModuleId);
            var rp = rolePermissions.FirstOrDefault(r => r.ModuleId == mod.ModuleId);

            bool access = uo?.CanAccess ?? rp?.CanAccess ?? false;
            bool create = uo?.CanCreate ?? rp?.CanCreate ?? false;
            bool update = uo?.CanUpdate ?? rp?.CanUpdate ?? false;
            bool toggle = uo?.CanToggleActive ?? rp?.CanToggleActive ?? false;

            result.Add(new ModuleEffectivePermissionViewModel(
                mod.ModuleKey,
                mod.ModuleNameAr,
                access,
                create,
                update,
                toggle
            ));
        }

        return result;
    }
}
