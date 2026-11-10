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

    [MaxLength(128)]
    public string? PasswordResetCodeHash { get; set; }

    public DateTime? PasswordResetCodeExpiryUtc { get; set; }

    public int PasswordResetFailedAttempts { get; set; } = 0;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserPermissionOverride> PermissionOverrides { get; set; } = new List<UserPermissionOverride>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
