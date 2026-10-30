using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class RolePermission
{
    [Key]
    public int RolePermissionId { get; set; }

    public int JobTitleId { get; set; }

    [ForeignKey(nameof(JobTitleId))]
    public virtual JobTitle? JobTitle { get; set; }

    public int ModuleId { get; set; }

    [ForeignKey(nameof(ModuleId))]
    public virtual SystemModule? Module { get; set; }

    public bool CanAccess { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanToggleActive { get; set; }
}
