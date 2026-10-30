using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CentralPress.API.Entities;

public class UserPermissionOverride
{
    [Key]
    public int OverrideId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public int ModuleId { get; set; }

    [ForeignKey(nameof(ModuleId))]
    public virtual SystemModule? Module { get; set; }

    public bool? CanAccess { get; set; }
    public bool? CanCreate { get; set; }
    public bool? CanUpdate { get; set; }
    public bool? CanToggleActive { get; set; }
}
