using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CentralPress.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public record ModuleEffectivePermissionDto(
    string ModuleKey,
    string ModuleNameAr,
    bool CanAccess,
    bool CanCreate,
    bool CanUpdate,
    bool CanToggleActive
);

public class PermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(string userId, string moduleKey, string actionType)
    {
        var user = await _context.Users
            .Include(u => u.PermissionOverrides)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive) return false;

        var module = await _context.SystemModules.FirstOrDefaultAsync(m => m.ModuleKey == moduleKey && m.IsActive);
        if (module == null) return false;

        // 1. Check User Override
        var userOverride = user.PermissionOverrides.FirstOrDefault(o => o.ModuleId == module.ModuleId);
        if (userOverride != null)
        {
            bool? overrideVal = actionType switch
            {
                "CanAccess" => userOverride.CanAccess,
                "CanCreate" => userOverride.CanCreate,
                "CanUpdate" => userOverride.CanUpdate,
                "CanToggleActive" => userOverride.CanToggleActive,
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
                return actionType switch
                {
                    "CanAccess" => rolePerm.CanAccess,
                    "CanCreate" => rolePerm.CanCreate,
                    "CanUpdate" => rolePerm.CanUpdate,
                    "CanToggleActive" => rolePerm.CanToggleActive,
                    _ => false
                };
            }
        }

        return false;
    }

    public async Task<List<ModuleEffectivePermissionDto>> GetUserPermissionsAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.PermissionOverrides)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var modules = await _context.SystemModules.Where(m => m.IsActive).ToListAsync();
        if (user == null) return new List<ModuleEffectivePermissionDto>();

        var rolePermissions = user.JobTitleId.HasValue
            ? await _context.RolePermissions.Where(rp => rp.JobTitleId == user.JobTitleId.Value).ToListAsync()
            : new List<Entities.RolePermission>();

        var result = new List<ModuleEffectivePermissionDto>();

        foreach (var mod in modules)
        {
            var uo = user.PermissionOverrides.FirstOrDefault(o => o.ModuleId == mod.ModuleId);
            var rp = rolePermissions.FirstOrDefault(r => r.ModuleId == mod.ModuleId);

            bool access = uo?.CanAccess ?? rp?.CanAccess ?? false;
            bool create = uo?.CanCreate ?? rp?.CanCreate ?? false;
            bool update = uo?.CanUpdate ?? rp?.CanUpdate ?? false;
            bool toggle = uo?.CanToggleActive ?? rp?.CanToggleActive ?? false;

            result.Add(new ModuleEffectivePermissionDto(
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
