using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CentralPress.API.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public int? JobTitleId { get; set; }

    [ForeignKey(nameof(JobTitleId))]
    public virtual JobTitle? JobTitle { get; set; }

    public bool MustChangePassword { get; set; } = false;

    public bool IsActive { get; set; } = true;

    [MaxLength(10)]
    public string? PasswordResetCode { get; set; }

    public DateTime? PasswordResetCodeExpiryUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CurrentSessionId { get; set; }

    public DateTime? LastActivityUtc { get; set; }

    public DateTime? SessionStartedUtc { get; set; }

    public virtual ICollection<UserPermissionOverride> PermissionOverrides { get; set; } = new List<UserPermissionOverride>();
}
