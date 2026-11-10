using System.Threading;
using System.Threading.Tasks;
using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IAuthService
{
    Task<LoginResponseViewModel?> LoginAsync(LoginDto dto, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthTokenViewModel?> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> ChangePasswordAsync(string userId, ChangePasswordDto dto, string? ipAddress, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message, UserProfileViewModel? User)> UpdateProfileAsync(string userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message, bool EmailSent, string? EmailError)> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> VerifyResetCodeAsync(VerifyResetCodeDto dto, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string Message)> ResetPasswordAsync(ResetPasswordDto dto, string? ipAddress, CancellationToken cancellationToken = default);
    Task<object?> GetCurrentUserAsync(string? userId, string? email, bool isSuperAdmin, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? userId, CancellationToken cancellationToken = default);
    Task TerminateSessionAsync(string? token, CancellationToken cancellationToken = default);
}
