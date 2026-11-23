using System;
using System.Text.Json;
using System.Threading.Tasks;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CentralPress.API.Services;

/// <summary>
/// Hangfire background job responsible for retrying failed email dispatches with exponential delays.
/// </summary>
public class EmailRetryJob : IEmailRetryJob
{
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailRetryJob> _logger;

    public EmailRetryJob(
        IEmailService emailService,
        ApplicationDbContext context,
        ILogger<EmailRetryJob> logger)
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    [Queue("emails")]
    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 30, 120, 300, 900, 1800 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendEmailWithRetryAsync(string recipientEmail, string recipientName, string subject, string bodyHtml)
    {
        _logger.LogInformation("[HANGFIRE EMAIL RETRY] Attempting background delivery to {Email} ({Subject})", recipientEmail, subject);

        var (success, error) = await _emailService.SendDirectSmtpAsync(recipientEmail, recipientName, subject, bodyHtml);

        try
        {
            _context.AuditBacklogs.Add(new AuditBacklog
            {
                UserId = "Hangfire",
                EmployeeName = "مجدول البريد الخلفي (Hangfire)",
                JobTitleName = "Background Worker",
                ActionType = "EmailRetry",
                ModuleName = "EMAIL",
                EntityName = "EmailMessage",
                RecordId = recipientEmail,
                RequestPath = $"smtp://{recipientEmail}",
                StatusCode = success ? 200 : 502,
                IsSuccess = success,
                ErrorMessage = success ? null : (error?.Length > 1000 ? error[..1000] : error),
                NewValuesJson = JsonSerializer.Serialize(new { recipientEmail, subject, success }),
                TimestampUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Could not persist AuditBacklog record for EmailRetry job.");
        }

        if (!success)
        {
            throw new InvalidOperationException(error ?? $"Failed to deliver email to {recipientEmail} via SMTP.");
        }
    }
}
