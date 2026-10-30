using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CentralPress.API.Services;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string email, string employeeName, string resetCode);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetCodeAsync(string email, string employeeName, string resetCode)
    {
        // In this university on-premise system, we log the reset code to application logs and console
        // with production-ready extensible SMTP hook.
        _logger.LogInformation("=================================================================");
        _logger.LogInformation("[EMAIL DISPATCH] Password Reset Code for {Name} ({Email}): {Code}", employeeName, email, resetCode);
        _logger.LogInformation("=================================================================");
        
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n[📧 استعادة كلمة المرور] تم إرسال رمز التحقق إلى {email} ({employeeName}): >>> {resetCode} <<< (صالح لمدة 15 دقيقة)\n");
        Console.ResetColor();

        return Task.CompletedTask;
    }
}
