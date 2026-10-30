using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CentralPress.API.Entities;

public class JobTitle
{
    [Key]
    public int JobTitleId { get; set; }

    [Required]
    [MaxLength(150)]
    public string TitleNameAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
