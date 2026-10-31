using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CentralPress.API.Services;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string email, string employeeName, string resetCode);
    Task SendNewUserInvitationCodeAsync(string email, string employeeName, string employeeCode, string initialCode);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _config;

    public EmailService(ILogger<EmailService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendPasswordResetCodeAsync(string email, string employeeName, string resetCode)
    {
        _logger.LogInformation("=================================================================");
        _logger.LogInformation("[EMAIL DISPATCH] Password Reset Code for {Name} ({Email}): {Code}", employeeName, email, resetCode);
        _logger.LogInformation("=================================================================");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n[📧 استعادة كلمة المرور] تم توليد رمز التحقق إلى {email} ({employeeName}): >>> {resetCode} <<< (صالح لمدة 15 دقيقة)\n");
        Console.ResetColor();

        var subject = "مطبعة جامعة أسيوط - رمز استعادة كلمة المرور";
        var bodyHtml = $@"
            <div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;"">
                <div style=""text-align: center; border-bottom: 2px solid #0f766e; padding-bottom: 16px; margin-bottom: 20px;"">
                    <h2 style=""color: #0f766e; margin: 0;"">مطبعة جامعة أسيوط المركزية</h2>
                    <p style=""color: #64748b; font-size: 13px; margin: 4px 0 0 0;"">النظام المحاسبي والإداري المركزي</p>
                </div>
                <p style=""font-size: 15px; color: #1e293b;"">مرحباً <strong>{employeeName}</strong>،</p>
                <p style=""font-size: 14px; color: #334155;"">لقد تلقينا طلباً لاستعادة كلمة المرور الخاصة بحسابك. استخدم الرمز التالي لإتمام عملية إعادة التعيين:</p>
                <div style=""text-align: center; margin: 24px 0;"">
                    <span style=""display: inline-block; background-color: #f1f5f9; border: 2px dashed #0f766e; color: #0f766e; font-size: 28px; font-weight: bold; letter-spacing: 4px; padding: 12px 28px; border-radius: 8px; font-family: monospace;"">{resetCode}</span>
                </div>
                <p style=""font-size: 12px; color: #94a3b8; text-align: center;"">هذا الرمز صالح لمدة 15 دقيقة فقط. إذا لم تكن أنت من طلب هذا الرمز، يرجى تجاهل هذه الرسالة.</p>
            </div>";

        await TrySendSmtpEmailAsync(email, employeeName, subject, bodyHtml);
    }

    public async Task SendNewUserInvitationCodeAsync(string email, string employeeName, string employeeCode, string initialCode)
    {
        _logger.LogInformation("=================================================================");
        _logger.LogInformation("[EMAIL DISPATCH] New Employee Account Invitation: {Name} | Code: {EmpCode} | Email: {Email} | Initial Password: {Password}", employeeName, employeeCode, email, initialCode);
        _logger.LogInformation("=================================================================");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[📧 دعوة موظف جديد] تم توليد كود التفعيل والدخول إلى {email} ({employeeName}):");
        Console.WriteLine($"   كود الموظف: {employeeCode}");
        Console.WriteLine($"   رمز الدخول المبدئي (كلمة المرور المؤقتة): >>> {initialCode} <<<");
        Console.WriteLine($"   (ملاحظة: سيُطلب من الموظف تعيين كلمة مرور شخصية جديدة وتأكيدها فور الدخول)\n");
        Console.ResetColor();

        var subject = "مطبعة جامعة أسيوط - بيانات حسابك الجديد للدخول للنظام";
        var bodyHtml = $@"
            <div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;"">
                <div style=""text-align: center; border-bottom: 2px solid #0f766e; padding-bottom: 16px; margin-bottom: 20px;"">
                    <h2 style=""color: #0f766e; margin: 0;"">مطبعة جامعة أسيوط المركزية</h2>
                    <p style=""color: #64748b; font-size: 13px; margin: 4px 0 0 0;"">النظام المحاسبي والإداري المركزي</p>
                </div>
                <p style=""font-size: 15px; color: #1e293b;"">مرحباً بالزميل <strong>{employeeName}</strong>،</p>
                <p style=""font-size: 14px; color: #334155;"">تم إنشاء حساب وظيفي جديد لك على منظومة مطبعة جامعة أسيوط. فيما يلي بيانات الدخول المبدئية الخاصة بك:</p>
                <div style=""background-color: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 16px; margin: 20px 0; font-size: 14px;"">
                    <div style=""margin-bottom: 8px;""><strong>كود الموظف:</strong> <span style=""font-family: monospace; color: #0f766e; font-weight: bold;"">{employeeCode}</span></div>
                    <div style=""margin-bottom: 8px;""><strong>البريد الإلكتروني:</strong> <span style=""font-family: monospace;"">{email}</span></div>
                    <div><strong>كلمة المرور المبدئية:</strong> <span style=""font-family: monospace; color: #b91c1c; font-weight: bold; font-size: 16px;"">{initialCode}</span></div>
                </div>
                <p style=""font-size: 13px; color: #475569; line-height: 1.6;"">
                    ⚠️ <strong>تنبيه أمني هام:</strong> هذه كلمة مرور مؤقتة للاستخدام لمرة واحدة فقط. بمجرد تسجيل الدخول، سيطلب منك النظام مباشرة تعيين كلمة مرور شخصية جديدة وتأكيدها.
                </p>
                <div style=""text-align: center; margin-top: 24px; padding-top: 16px; border-top: 1px solid #f1f5f9; color: #94a3b8; font-size: 12px;"">
                    إدارة نظم المعلومات والتحول الرقمي • مطبعة جامعة أسيوط
                </div>
            </div>";

        await TrySendSmtpEmailAsync(email, employeeName, subject, bodyHtml);
    }

    private async Task TrySendSmtpEmailAsync(string recipientEmail, string recipientName, string subject, string htmlContent)
    {
        try
        {
            var host = _config["SmtpSettings:Host"];
            var portStr = _config["SmtpSettings:Port"];
            var enableSsl = bool.TryParse(_config["SmtpSettings:EnableSsl"], out var ssl) && ssl;
            var userName = _config["SmtpSettings:UserName"];
            var password = _config["SmtpSettings:Password"];
            var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "no-reply@aup.edu.eg";
            var senderName = _config["SmtpSettings:SenderName"] ?? "مطبعة جامعة أسيوط المركزية";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("[SMTP SKIPPED] Host, UserName or Password not configured in appsettings.json. Credentials logged safely to console/database.");
                return;
            }

            int port = 587;
            if (!string.IsNullOrWhiteSpace(portStr)) int.TryParse(portStr, out port);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password),
                Timeout = 10000
            };

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName, Encoding.UTF8),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(recipientEmail, recipientName, Encoding.UTF8));

            await client.SendMailAsync(message);
            _logger.LogInformation("[SMTP SUCCESS] Email successfully sent to {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[SMTP WARNING] Could not dispatch email via SMTP ({Message}). The user can still log in using the initial code displayed in the admin UI / logs.", ex.Message);
        }
    }
}
