using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuperAdminController : ControllerBase
{
    private readonly SuperAdminTotpValidator _totpValidator;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public SuperAdminController(
        SuperAdminTotpValidator totpValidator,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _totpValidator = totpValidator;
        _context = context;
        _configuration = configuration;
    }

    public record SuperAdminLoginDto(string Code);

    [HttpPost("login")]
    public IActionResult Login([FromBody] SuperAdminLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "يرجى إدخال رمز التحقق الساعي للـ Super Admin" });

        bool isValid = _totpValidator.ValidateCode(dto.Code);
        if (!isValid)
        {
            return Unauthorized(new { message = "رمز التحقق الساعي غير صحيح أو انتهت صلاحية الساعة الحالية. يرجى الحصول على الرمز الجديد من تطبيق 3-superadmin-keygen" });
        }

        // Generate SuperAdmin JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:SecurityKey"] ?? "ASSIUT_UNIV_CENTRAL_PRESS_JWT_KEY_SUPER_SECRET_1234567890!@#$%"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "SUPERADMIN_ROOT"),
            new Claim(ClaimTypes.Email, "superadmin@aup.edu.eg"),
            new Claim("EmployeeCode", "SUPERADMIN"),
            new Claim("FullName", "المسؤول الفائق (Super Admin)"),
            new Claim("JobTitle", "Super Administrator"),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "CentralPressAPI",
            audience: _configuration["JwtSettings:Audience"] ?? "CentralPressClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        string jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = jwt,
            user = new
            {
                Id = "SUPERADMIN_ROOT",
                EmployeeCode = "SUPERADMIN",
                FullName = "المسؤول الفائق (Super Admin)",
                Email = "superadmin@aup.edu.eg",
                JobTitleName = "Super Administrator",
                MustChangePassword = false,
                IsSuperAdmin = true
            }
        });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("backlog")]
    public async Task<IActionResult> GetAuditBacklog(
        [FromQuery] string? search,
        [FromQuery] string? moduleName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditBacklogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            query = query.Where(b => b.ModuleName == moduleName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b =>
                (b.EmployeeName != null && b.EmployeeName.Contains(search)) ||
                (b.ActionType != null && b.ActionType.Contains(search)) ||
                (b.RequestPath != null && b.RequestPath.Contains(search)) ||
                (b.NewValuesJson != null && b.NewValuesJson.Contains(search))
            );
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            items
        });
    }
}
