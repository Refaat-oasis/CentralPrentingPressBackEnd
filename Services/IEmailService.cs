using System.Threading.Tasks;

namespace CentralPress.API.Services;

public interface IEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendPasswordResetCodeAsync(string email, string employeeName, string resetCode);
    Task<(bool Success, string? ErrorMessage)> SendNewUserInvitationCodeAsync(string email, string employeeName, string employeeCode, string initialCode);
    Task<(bool Success, string Message, string? Details)> TestSmtpConnectionAsync(string testRecipientEmail);
}
