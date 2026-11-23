using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.Services;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "cp_rt";
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _configuration = configuration;
        _environment = environment;
    }

    private CookieOptions BuildRefreshCookieOptions(DateTimeOffset? expires = null)
    {
        bool requireHttps = Request.IsHttps ||
            (!_environment.IsDevelopment() && _configuration.GetValue<bool>("Security:RequireHttps", true));

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = requireHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = expires ?? DateTimeOffset.UtcNow.AddDays(7)
        };
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "يرجى إدخال اسم المستخدم / الكود وكلمة المرور" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(dto, ipAddress, cancellationToken);
        if (result == null)
            return Unauthorized(new { message = "بيانات الدخول غير صحيحة أو الحساب غير مفعل أو مقيد مؤقتاً" });

        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            Response.Cookies.Append(RefreshCookieName, result.RefreshToken, BuildRefreshCookieOptions());
        }

        return Ok(new LoginResponseViewModel
        {
            Token = result.Token,
            RefreshToken = null,
            User = result.User,
            Permissions = result.Permissions
        });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> RefreshToken(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies.TryGetValue(RefreshCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken)
            ? cookieToken
            : dto?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { message = "رمز التحديث مفقود" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, cancellationToken);
        if (result == null)
        {
            Response.Cookies.Delete(RefreshCookieName, BuildRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
            return Unauthorized(new { message = "رمز التحديث غير صالح أو منتهي الصلاحية" });
        }

        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            Response.Cookies.Append(RefreshCookieName, result.RefreshToken, BuildRefreshCookieOptions());
        }

        return Ok(new
        {
            token = result.AccessToken,
            accessToken = result.AccessToken,
            expiresAtUtc = result.ExpiresAtUtc
        });
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RevokeTokenRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies.TryGetValue(RefreshCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken)
            ? cookieToken
            : dto?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest(new { message = "رمز التحديث مطلوب" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var succeeded = await _authService.RevokeTokenAsync(refreshToken, ipAddress, cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, BuildRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));

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
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        var (_, message, _, _) = await _authService.ForgotPasswordAsync(dto, cancellationToken);
        return Ok(new { message, emailSent = true });
    }

    [HttpPost("verify-reset-code")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto, CancellationToken cancellationToken)
    {
        var (succeeded, message) = await _authService.VerifyResetCodeAsync(dto, cancellationToken);
        if (!succeeded)
            return BadRequest(new { message });

        return Ok(new { message, valid = true });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthRateLimit")]
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        var result = await _authService.GetCurrentUserAsync(userId, email, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await _authService.LogoutAsync(userId, cancellationToken);
        }

        if (Request.Cookies.TryGetValue(RefreshCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.RevokeTokenAsync(cookieToken, ipAddress, cancellationToken);
        }
        Response.Cookies.Delete(RefreshCookieName, BuildRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));

        return Ok(new { message = "تم تسجيل الخروج بنجاح وتحرير الجلسة" });
    }

    [Authorize]
    [HttpPost("terminate-session")]
    public async Task<IActionResult> TerminateSession(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = User.FindFirstValue("SessionId");

        await _authService.TerminateSessionAsync(userId, sessionId, cancellationToken);

        if (Request.Cookies.TryGetValue(RefreshCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.RevokeTokenAsync(cookieToken, ipAddress, cancellationToken);
        }
        Response.Cookies.Delete(RefreshCookieName, BuildRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));

        return Ok(new { terminated = true });
    }

    [Authorize]
    [HttpGet("heartbeat")]
    public IActionResult Heartbeat()
    {
        return Ok(new { active = true });
    }
}
