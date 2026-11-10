using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "يرجى إدخال اسم المستخدم / الكود وكلمة المرور" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(dto, ipAddress, cancellationToken);
        if (result == null)
            return Unauthorized(new { message = "بيانات الدخول غير صحيحة أو الحساب غير مفعل" });

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "رمز التحديث مطلوب" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken, ipAddress, cancellationToken);
        if (result == null)
            return Unauthorized(new { message = "رمز التحديث غير صالح أو منتهي الصلاحية" });

        return Ok(result);
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "رمز التحديث مطلوب" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var succeeded = await _authService.RevokeTokenAsync(dto.RefreshToken, ipAddress, cancellationToken);
        if (!succeeded)
            return NotFound(new { message = "رمز التحديث غير موجود أو تم إبطاله بالفعل" });

        return Ok(new { message = "تم إلغاء صلاحية رمز التحديث بنجاح" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (succeeded, message) = await _authService.ChangePasswordAsync(userId, dto, ipAddress, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var (succeeded, message, user) = await _authService.UpdateProfileAsync(userId, dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, user });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message, emailSent, emailError) = await _authService.ForgotPasswordAsync(dto, cancellationToken);
        return Ok(new { message, emailSent, emailError });
    }

    [HttpPost("verify-reset-code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _authService.VerifyResetCodeAsync(dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, valid = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (succeeded, message) = await _authService.ResetPasswordAsync(dto, ipAddress, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        var result = await _authService.GetCurrentUserAsync(userId, email, isSuperAdmin, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }
}
