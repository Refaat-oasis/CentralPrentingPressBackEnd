using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CentralPress.API.Services;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly byte[] _keyBytes;

    public TokenService(
        ApplicationDbContext context,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;

        var secret = _configuration["JwtSettings:SecurityKey"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("JwtSettings:SecurityKey is not properly configured (minimum 32 characters required).");
        }
        _keyBytes = Encoding.UTF8.GetBytes(secret);
    }

    public async Task<AuthTokenViewModel> GenerateTokensAsync(ApplicationUser user, bool isSuperAdmin, string? ipAddress)
    {
        var roles = isSuperAdmin ? new List<string> { "SuperAdmin" } : (await _userManager.GetRolesAsync(user)).ToList();
        if (!roles.Any() && !isSuperAdmin)
        {
            roles.Add("Employee");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("EmployeeCode", user.EmployeeCode ?? string.Empty),
            new("FullName", user.FullName ?? string.Empty),
            new("JobTitle", user.JobTitle?.TitleNameAr ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Collect role claims
        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var identityRole = await _roleManager.FindByNameAsync(role);
            if (identityRole != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(identityRole);
                foreach (var rc in roleClaims.Where(c => c.Type == "permission"))
                {
                    permissionSet.Add(rc.Value);
                }
            }
        }

        // Collect direct user claims only for database-persisted users
        if (!isSuperAdmin)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            foreach (var uc in userClaims.Where(c => c.Type == "permission"))
            {
                permissionSet.Add(uc.Value);
            }
        }

        foreach (var perm in permissionSet)
        {
            claims.Add(new Claim("permission", perm));
        }

        var expiresMinutes = double.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var m) ? m : 30;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var key = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            Issuer = _configuration["JwtSettings:Issuer"] ?? "CentralPressAPI",
            Audience = _configuration["JwtSettings:Audience"] ?? "CentralPressClient",
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(tokenDescriptor);
        var accessToken = handler.WriteToken(securityToken);

        // Generate Refresh Token ONLY for standard persisted users
        // SuperAdmin relies strictly on hourly rotating TOTP codes without persistent DB sessions
        string refreshTokenString = string.Empty;
        if (!isSuperAdmin)
        {
            refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenString,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
        }

        return new AuthTokenViewModel
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public async Task<AuthTokenViewModel?> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var existingToken = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.JobTitle)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (existingToken == null)
            return null;

        // Replay Attack Detection: If token is already revoked, compromise detected!
        if (existingToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(existingToken.UserId, ipAddress);
            return null;
        }

        if (existingToken.IsExpired)
            return null;

        var user = existingToken.User;
        if (user == null || !user.IsActive)
            return null;

        // Rotate token
        var newRefreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.ReplacedByToken = newRefreshTokenString;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        // Issue new JWT
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        if (!roles.Any()) roles.Add("Employee");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("EmployeeCode", user.EmployeeCode ?? string.Empty),
            new("FullName", user.FullName ?? string.Empty),
            new("JobTitle", user.JobTitle?.TitleNameAr ?? string.Empty)
        };
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var identityRole = await _roleManager.FindByNameAsync(role);
            if (identityRole != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(identityRole);
                foreach (var rc in roleClaims.Where(c => c.Type == "permission"))
                {
                    permissionSet.Add(rc.Value);
                }
            }
        }
        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var uc in userClaims.Where(c => c.Type == "permission"))
        {
            permissionSet.Add(uc.Value);
        }
        foreach (var perm in permissionSet)
        {
            claims.Add(new Claim("permission", perm));
        }

        var expiresMinutes = double.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var m) ? m : 30;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var key = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            Issuer = _configuration["JwtSettings:Issuer"] ?? "CentralPressAPI",
            Audience = _configuration["JwtSettings:Audience"] ?? "CentralPressClient",
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(tokenDescriptor);
        var newAccessToken = handler.WriteToken(securityToken);

        return new AuthTokenViewModel
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token == null || !token.IsActive)
            return false;

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RevokeAllUserTokensAsync(string userId, string? ipAddress)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        foreach (var t in tokens)
        {
            t.RevokedAtUtc = DateTime.UtcNow;
            t.RevokedByIp = ipAddress;
        }

        await _context.SaveChangesAsync();
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
