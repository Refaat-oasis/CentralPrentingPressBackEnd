using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.DTOs;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CentralPress.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly PermissionService _permissionService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ITokenService tokenService,
        IEmailService emailService,
        PermissionService permissionService)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _permissionService = permissionService;
    }

    public async Task<LoginResponseViewModel?> LoginAsync(LoginDto dto, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Password))
            return null;

        var user = await _userManager.FindByEmailAsync(dto.Identifier)
                   ?? await _context.Users
                       .Include(u => u.JobTitle)
                       .FirstOrDefaultAsync(u => u.EmployeeCode == dto.Identifier, cancellationToken);

        if (user == null || !user.IsActive)
            return null;

        bool passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
            return null;

        var tokens = await _tokenService.GenerateTokensAsync(user, isSuperAdmin: false, ipAddress);
        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);

        var jobTitle = user.JobTitle ?? (user.JobTitleId.HasValue ? await _context.JobTitles.FindAsync([user.JobTitleId.Value], cancellationToken) : null);

        var userProfile = new UserProfileViewModel
        {
            Id = user.Id,
            EmployeeCode = user.EmployeeCode,
            FullName = user.FullName,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl,
            JobTitleId = user.JobTitleId,
            JobTitleName = jobTitle?.TitleNameAr,
            MustChangePassword = user.MustChangePassword,
            IsSuperAdmin = false
        };

        return new LoginResponseViewModel
        {
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            User = userProfile,
            Permissions = permissions
        };
    }

    public async Task<AuthTokenViewModel?> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        return await _tokenService.RefreshTokenAsync(refreshToken, ipAddress);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        return await _tokenService.RevokeTokenAsync(refreshToken, ipAddress);
    }

    public async Task<(bool Succeeded, string Message)> ChangePasswordAsync(string userId, ChangePasswordDto dto, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "المستخدم غير موجود");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        // Invalidate all tokens and refresh security stamp on password change
        await _userManager.UpdateSecurityStampAsync(user);
        await _tokenService.RevokeAllUserTokensAsync(user.Id, ipAddress);

        return (true, "تم تغيير كلمة المرور بنجاح");
    }

    public async Task<(bool Succeeded, string Message, UserProfileViewModel? User)> UpdateProfileAsync(string userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return (false, "المستخدم غير موجود", null);

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return (false, "الاسم الكامل مطلوب", null);

        user.FullName = dto.FullName.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        var profile = new UserProfileViewModel
        {
            Id = user.Id,
            EmployeeCode = user.EmployeeCode,
            FullName = user.FullName,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl,
            JobTitleId = user.JobTitleId,
            JobTitleName = user.JobTitle?.TitleNameAr,
            MustChangePassword = user.MustChangePassword,
            IsSuperAdmin = false
        };

        return (true, "تم تحديث الملف الشخصي بنجاح", profile);
    }

    public async Task<(bool Succeeded, string Message, bool EmailSent, string? EmailError)> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            // Do not disclose user existence to prevent account enumeration
            return (true, "إذا كان البريد مسجلاً، تم إرسال رمز التحقق", true, null);
        }

        // Cryptographically secure 6-digit OTP generation
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var codeHash = ComputeOtpHash(user.Email!, code);

        user.PasswordResetCodeHash = codeHash;
        user.PasswordResetCodeExpiryUtc = DateTime.UtcNow.AddMinutes(15);
        user.PasswordResetFailedAttempts = 0;
        await _userManager.UpdateAsync(user);

        var (emailSent, emailError) = await _emailService.SendPasswordResetCodeAsync(user.Email!, user.FullName, code);

        if (!emailSent)
        {
            return (true, "تم توليد رمز التحقق، ولكن تعذر تسليمه للبريد الإلكتروني (يرجى مراجعة إعدادات خادم البريد SMTP)", false, emailError);
        }

        return (true, "تم إرسال رمز التحقق بنجاح إلى البريد الإلكتروني", true, null);
    }

    public async Task<(bool Succeeded, string Message)> VerifyResetCodeAsync(VerifyResetCodeDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || string.IsNullOrEmpty(user.PasswordResetCodeHash) || !user.PasswordResetCodeExpiryUtc.HasValue)
        {
            return (false, "رمز التحقق غير صحيح أو انتهت صلاحيته");
        }

        if (user.PasswordResetCodeExpiryUtc.Value < DateTime.UtcNow)
        {
            user.PasswordResetCodeHash = null;
            user.PasswordResetCodeExpiryUtc = null;
            await _userManager.UpdateAsync(user);
            return (false, "انتهت صلاحية رمز التحقق");
        }

        // Throttle failed attempts (Lockout after 3 failed guesses)
        if (user.PasswordResetFailedAttempts >= 3)
        {
            user.PasswordResetCodeHash = null;
            user.PasswordResetCodeExpiryUtc = null;
            await _userManager.UpdateAsync(user);
            return (false, "تم تجاوز الحد الأقصى للمحاولات الخاطئة. يرجى طلب رمز جديد.");
        }

        var expectedHash = ComputeOtpHash(user.Email!, dto.Code);
        bool matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(user.PasswordResetCodeHash),
            Encoding.UTF8.GetBytes(expectedHash));

        if (!matches)
        {
            user.PasswordResetFailedAttempts++;
            await _userManager.UpdateAsync(user);
            return (false, "رمز التحقق غير صحيح أو انتهت صلاحيته");
        }

        return (true, "رمز التحقق صحيح");
    }

    public async Task<(bool Succeeded, string Message)> ResetPasswordAsync(ResetPasswordDto dto, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || string.IsNullOrEmpty(user.PasswordResetCodeHash) || !user.PasswordResetCodeExpiryUtc.HasValue)
        {
            return (false, "رمز التحقق غير صحيح أو انتهت صلاحيته");
        }

        if (user.PasswordResetCodeExpiryUtc.Value < DateTime.UtcNow)
        {
            user.PasswordResetCodeHash = null;
            user.PasswordResetCodeExpiryUtc = null;
            await _userManager.UpdateAsync(user);
            return (false, "انتهت صلاحية رمز التحقق");
        }

        if (user.PasswordResetFailedAttempts >= 3)
        {
            user.PasswordResetCodeHash = null;
            user.PasswordResetCodeExpiryUtc = null;
            await _userManager.UpdateAsync(user);
            return (false, "تم تجاوز الحد الأقصى للمحاولات الخاطئة. يرجى طلب رمز جديد.");
        }

        var expectedHash = ComputeOtpHash(user.Email!, dto.Code);
        bool matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(user.PasswordResetCodeHash),
            Encoding.UTF8.GetBytes(expectedHash));

        if (!matches)
        {
            user.PasswordResetFailedAttempts++;
            await _userManager.UpdateAsync(user);
            return (false, "رمز التحقق غير صحيح أو انتهت صلاحيته");
        }

        // Generate Identity token and reset password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Invalidate reset code immediately upon successful use (MED-03)
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiryUtc = null;
        user.PasswordResetFailedAttempts = 0;
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        // Invalidate existing sessions
        await _userManager.UpdateSecurityStampAsync(user);
        await _tokenService.RevokeAllUserTokensAsync(user.Id, ipAddress);

        return (true, "تم إعادة تعيين كلمة المرور بنجاح");
    }

    public async Task<object?> GetCurrentUserAsync(string? userId, string? email, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        if (isSuperAdmin)
        {
            var modules = await _context.SystemModules.ToListAsync(cancellationToken);
            var permissions = modules.Select(m => new ModuleEffectivePermissionViewModel(
                m.ModuleKey,
                m.ModuleNameAr,
                true,
                true,
                true,
                true
            ));

            return new
            {
                user = new UserProfileViewModel
                {
                    Id = "SUPERADMIN_ROOT",
                    EmployeeCode = "SUPERADMIN",
                    FullName = "Super Administrator (المسؤول الفائق)",
                    Email = "superadmin@aup.edu.eg",
                    JobTitleName = "Super Admin Authority",
                    MustChangePassword = false,
                    IsSuperAdmin = true
                },
                permissions
            };
        }

        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(email)) return null;

        var user = await _context.Users
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => (userId != null && u.Id == userId) || (email != null && u.Email == email), cancellationToken);

        if (user == null) return null;

        var userPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);

        return new
        {
            user = new UserProfileViewModel
            {
                Id = user.Id,
                EmployeeCode = user.EmployeeCode,
                FullName = user.FullName,
                Email = user.Email,
                PhotoUrl = user.PhotoUrl,
                JobTitleId = user.JobTitleId,
                JobTitleName = user.JobTitle?.TitleNameAr,
                MustChangePassword = user.MustChangePassword,
                IsSuperAdmin = false
            },
            permissions = userPermissions
        };
    }

    public async Task LogoutAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.CurrentSessionId = null;
                user.LastActivityUtc = null;
                await _userManager.UpdateAsync(user);
            }
        }
    }

    public async Task TerminateSessionAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token)) return;

        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
                var sessionId = jwt.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null && (sessionId == null || user.CurrentSessionId == sessionId))
                    {
                        user.CurrentSessionId = null;
                        user.LastActivityUtc = null;
                        await _userManager.UpdateAsync(user);
                    }
                }
            }
        }
        catch
        {
            // Ignore any error during browser unload
        }
    }

    private static string ComputeOtpHash(string email, string code)
    {
        var input = $"{email.ToLowerInvariant()}:{code.Trim()}:CentralPressSecuritySalt2026";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }
}
