using System.Security.Claims;
using System.Threading.Tasks;
using CentralPress.API.Entities;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface ITokenService
{
    Task<AuthTokenViewModel> GenerateTokensAsync(ApplicationUser user, bool isSuperAdmin, string? ipAddress);
    Task<AuthTokenViewModel?> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeAllUserTokensAsync(string userId, string? ipAddress);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
