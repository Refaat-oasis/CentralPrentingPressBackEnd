using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.Entities;

public class SystemModule
{
    [Key]
    public int ModuleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ModuleKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ModuleNameAr { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserPermissionOverride> UserOverrides { get; set; } = new List<UserPermissionOverride>();
}
