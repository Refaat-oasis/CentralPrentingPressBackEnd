using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly PermissionService _permissionService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        PermissionService permissionService)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _permissionService = permissionService;
    }

    public record LoginDto(string Identifier, string Password);
    public record ChangePasswordDto(string CurrentPassword, string NewPassword);
    public record ForgotPasswordDto(string Email);
    public record VerifyResetCodeDto(string Email, string Code);
    public record ResetPasswordDto(string Email, string Code, string NewPassword);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "يرجى إدخال اسم المستخدم / الكود وكلمة المرور" });

        var user = await _userManager.FindByEmailAsync(dto.Identifier)
                   ?? await _context.Users.FirstOrDefaultAsync(u => u.EmployeeCode == dto.Identifier);

        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "بيانات الدخول غير صحيحة أو الحساب غير مفعل" });

        bool passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
            return Unauthorized(new { message = "كلمة المرور غير صحيحة" });

        // Generate new unique session ID
        var sessionId = Guid.NewGuid().ToString();
        user.CurrentSessionId = sessionId;
        user.LastActivityUtc = DateTime.UtcNow;
        user.SessionStartedUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        string token = GenerateJwtToken(user, isSuperAdmin: false, sessionId: sessionId);
        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.EmployeeCode,
                user.FullName,
                user.Email,
                user.PhotoUrl,
                user.JobTitleId,
                JobTitleName = (await _context.JobTitles.FindAsync(user.JobTitleId))?.TitleNameAr,
                user.MustChangePassword,
                IsSuperAdmin = false
            },
            permissions
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Ok(new { message = "إذا كان البريد مسجلاً، تم إرسال رمز التحقق" });

        // Generate 6-digit code
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        user.PasswordResetCode = code;
        user.PasswordResetCodeExpiryUtc = DateTime.UtcNow.AddMinutes(15);
        await _userManager.UpdateAsync(user);

        await _emailService.SendPasswordResetCodeAsync(user.Email!, user.FullName, code);

        return Ok(new { message = "تم إرسال رمز التحقق بنجاح إلى البريد الإلكتروني" });
    }

    [HttpPost("verify-reset-code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || user.PasswordResetCode != dto.Code || user.PasswordResetCodeExpiryUtc < DateTime.UtcNow)
        {
            return BadRequest(new { message = "رمز التحقق غير صحيح أو انتهت صلاحيته" });
        }

        return Ok(new { message = "رمز التحقق صحيح", valid = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || user.PasswordResetCode != dto.Code || user.PasswordResetCodeExpiryUtc < DateTime.UtcNow)
        {
            return BadRequest(new { message = "رمز التحقق غير صحيح أو انتهت صلاحيته" });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiryUtc = null;
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        if (isSuperAdmin)
        {
            return Ok(new
            {
                user = new
                {
                    Id = "SUPERADMIN_ROOT",
                    EmployeeCode = "SUPERADMIN",
                    FullName = "Super Administrator (المسؤول الفائق)",
                    Email = "superadmin@aup.edu.eg",
                    JobTitleName = "Super Admin Authority",
                    MustChangePassword = false,
                    IsSuperAdmin = true
                },
                permissions = (await _context.SystemModules.ToListAsync())
                    .Select(m => new ModuleEffectivePermissionDto(m.ModuleKey, m.ModuleNameAr, true, true, true, true))
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _context.Users.Include(u => u.JobTitle).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.EmployeeCode,
                user.FullName,
                user.Email,
                user.PhotoUrl,
                user.JobTitleId,
                JobTitleName = user.JobTitle?.TitleNameAr,
                user.MustChangePassword,
                IsSuperAdmin = false
            },
            permissions
        });
    }

    private string GenerateJwtToken(ApplicationUser user, bool isSuperAdmin, string? sessionId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:SecurityKey"] ?? "ASSIUT_UNIV_CENTRAL_PRESS_JWT_KEY_SUPER_SECRET_1234567890!@#$%"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("EmployeeCode", user.EmployeeCode ?? ""),
            new Claim("FullName", user.FullName ?? ""),
            new Claim("JobTitle", user.JobTitle?.TitleNameAr ?? ""),
            new Claim("SessionId", sessionId ?? user.CurrentSessionId ?? ""),
            new Claim(ClaimTypes.Role, isSuperAdmin ? "SuperAdmin" : "Employee")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "CentralPressAPI",
            audience: _configuration["JwtSettings:Audience"] ?? "CentralPressClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "1440")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && !User.IsInRole("SuperAdmin"))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.CurrentSessionId = null;
                user.LastActivityUtc = null;
                await _userManager.UpdateAsync(user);
            }
        }

        return Ok(new { message = "تم تسجيل الخروج بنجاح وتحرير الجلسة" });
    }

    [HttpPost("terminate-session")]
    public async Task<IActionResult> TerminateSession([FromQuery] string? token)
    {
        var tokenStr = token;
        if (string.IsNullOrEmpty(tokenStr) && Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            tokenStr = authHeader.ToString().Replace("Bearer ", "").Trim();
        }

        if (!string.IsNullOrEmpty(tokenStr))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(tokenStr))
                {
                    var jwt = handler.ReadJwtToken(tokenStr);
                    var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
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

        return Ok(new { terminated = true });
    }

    [Authorize]
    [HttpGet("heartbeat")]
    public IActionResult Heartbeat()
    {
        return Ok(new { active = true });
    }
}
