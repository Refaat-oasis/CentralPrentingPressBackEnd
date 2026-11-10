using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CentralPress.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _config;

    public EmailService(ILogger<EmailService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendPasswordResetCodeAsync(string email, string employeeName, string resetCode)
    {
        _logger.LogInformation("[EMAIL DISPATCH] Password Reset Code dispatched for {Name} ({Email})", employeeName, email);

        var subject = "مطبعة جامعة أسيوط - رمز استعادة كلمة المرور";
        var bodyHtml = $@"
            <div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, -apple-system, BlinkMacSystemFont, Roboto, sans-serif; max-width: 620px; margin: 20px auto; background-color: #ffffff; border: 1px solid #cbd5e1; border-radius: 18px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); color: #0f172a;"">
                <!-- Header Banner -->
                <div style=""background: linear-gradient(135deg, #064e3b 0%, #047857 50%, #0f766e 100%); padding: 28px 24px; text-align: center; color: #ffffff;"">
                    <div style=""display: inline-block; background-color: rgba(255,255,255,0.18); border: 1px solid rgba(255,255,255,0.3); border-radius: 9999px; padding: 5px 16px; margin-bottom: 10px; font-size: 12px; font-weight: bold; color: #ffffff; letter-spacing: 0.5px;"">
                        جامعة أسيوط • جمهورية مصر العربية
                    </div>
                    <h1 style=""color: #ffffff !important; margin: 0; font-size: 22px; font-weight: 900; letter-spacing: -0.5px;"">مطبعة جامعة أسيوط المركزية</h1>
                    <p style=""color: #a7f3d0 !important; font-size: 13px; margin: 6px 0 0 0; font-weight: 600;"">استعادة كلمة المرور وتأمين الحساب</p>
                </div>

                <!-- Body Content -->
                <div style=""padding: 30px 26px; background-color: #ffffff; color: #0f172a;"">
                    <p style=""font-size: 16px; color: #0f172a !important; margin: 0 0 12px 0; font-weight: 700; line-height: 1.5;"">
                        مرحباً <strong style=""color: #0f766e !important;"">{employeeName}</strong>،
                    </p>
                    <p style=""font-size: 14px; color: #334155 !important; line-height: 1.7; margin: 0 0 24px 0; font-weight: 500;"">
                        لقد تلقينا طلباً لاستعادة وتعيين كلمة المرور الخاصة بحسابك على منظومة المطبعة المركزية. يرجى استخدام رمز التحقق المؤقت التالي لإتمام العملية:
                    </p>

                    <!-- OTP Code Box -->
                    <div style=""background-color: #ecfdf5; border: 2px dashed #059669; border-radius: 16px; padding: 24px 16px; text-align: center; margin: 0 0 24px 0;"">
                        <div style=""font-size: 13px; font-weight: 800; color: #065f46 !important; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;"">
                            رمز التحقق المؤقت (OTP)
                        </div>
                        <div style=""font-family: monospace, Consolas, Courier, sans-serif; font-size: 36px; font-weight: 900; letter-spacing: 8px; color: #065f46 !important; background-color: #ffffff; border: 1px solid #a7f3d0; border-radius: 10px; display: inline-block; padding: 10px 24px; box-shadow: 0 2px 8px rgba(4,120,87,0.1);"">
                            {resetCode}
                        </div>
                        <div style=""font-size: 12px; font-weight: 700; color: #047857 !important; margin-top: 10px;"">
                            ⏱️ هذا الرمز صالح لمدة 15 دقيقة فقط للاستخدام لمرة واحدة
                        </div>
                    </div>

                    <p style=""font-size: 13px; color: #64748b !important; line-height: 1.6; margin: 0 0 8px 0; font-weight: 500;"">
                        إذا لم تكن أنت من قام بطلب استعادة كلمة المرور، يرجى تجاهل هذه الرسالة، فحسابك لا يزال آمناً تماماً.
                    </p>
                </div>

                <!-- Footer -->
                <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 18px 24px; text-align: center; color: #64748b; font-size: 12px;"">
                    <p style=""margin: 0 0 4px 0; font-weight: 700; color: #334155 !important;"">إدارة نظم المعلومات والتحول الرقمي • مطبعة جامعة أسيوط المركزية</p>
                    <p style=""margin: 0; color: #94a3b8 !important; font-size: 11px;"">هذه رسالة آلية تم إنشاؤها وتأمينها تلقائياً من خادم التطبيق المركزي.</p>
                </div>
            </div>";

        return await TrySendSmtpEmailAsync(email, employeeName, subject, bodyHtml);
    }

    public async Task<(bool Success, string? ErrorMessage)> SendNewUserInvitationCodeAsync(string email, string employeeName, string employeeCode, string initialCode)
    {
        _logger.LogInformation("[EMAIL DISPATCH] New Employee Account Invitation dispatched for {Name} ({Email}), Code: {EmpCode}", employeeName, email, employeeCode);

        var subject = "مطبعة جامعة أسيوط - بيانات حسابك الجديد للدخول للنظام";
        var bodyHtml = $@"
            <div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, -apple-system, BlinkMacSystemFont, Roboto, sans-serif; max-width: 620px; margin: 20px auto; background-color: #ffffff; border: 1px solid #cbd5e1; border-radius: 18px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); color: #0f172a;"">
                <!-- Header Banner -->
                <div style=""background: linear-gradient(135deg, #064e3b 0%, #047857 50%, #0f766e 100%); padding: 28px 24px; text-align: center; color: #ffffff;"">
                    <div style=""display: inline-block; background-color: rgba(255,255,255,0.18); border: 1px solid rgba(255,255,255,0.3); border-radius: 9999px; padding: 5px 16px; margin-bottom: 10px; font-size: 12px; font-weight: bold; color: #ffffff; letter-spacing: 0.5px;"">
                        جامعة أسيوط • جمهورية مصر العربية
                    </div>
                    <h1 style=""color: #ffffff !important; margin: 0; font-size: 22px; font-weight: 900; letter-spacing: -0.5px;"">مطبعة جامعة أسيوط المركزية</h1>
                    <p style=""color: #a7f3d0 !important; font-size: 13px; margin: 6px 0 0 0; font-weight: 600;"">المنظومة المحاسبية والإدارية المركزية الموحدة</p>
                </div>

                <!-- Body Content -->
                <div style=""padding: 30px 26px; background-color: #ffffff; color: #0f172a;"">
                    <p style=""font-size: 16px; color: #0f172a !important; margin: 0 0 12px 0; font-weight: 800; line-height: 1.5;"">
                        مرحباً بالزميل <strong style=""color: #0f766e !important;"">{employeeName}</strong>،
                    </p>
                    <p style=""font-size: 14px; color: #334155 !important; line-height: 1.7; margin: 0 0 22px 0; font-weight: 500;"">
                        تم إنشاء حساب وظيفي معتمد لك على منظومة مطبعة جامعة أسيوط. فيما يلي بيانات الدخول المبدئية الخاصة بك:
                    </p>

                    <!-- Main High-Contrast Credentials Table Card -->
                    <div style=""background-color: #f8fafc; border: 2px solid #0f766e; border-radius: 14px; padding: 18px 20px; margin: 0 0 22px 0; box-shadow: inset 0 1px 3px rgba(0,0,0,0.03);"">
                        <div style=""border-bottom: 2px solid #e2e8f0; padding-bottom: 10px; margin-bottom: 14px; font-size: 14px; font-weight: 900; color: #065f46 !important;"">
                            📋 بيانات تسجيل الدخول المعتمدة
                        </div>

                        <table style=""width: 100%; border-collapse: separate; border-spacing: 0 10px;"">
                            <!-- Row 1: Code -->
                            <tr>
                                <td style=""padding: 10px 14px; width: 145px; font-size: 14px; font-weight: 800; color: #1e293b !important; background-color: #f1f5f9; border-radius: 8px 0 0 8px; border: 1px solid #cbd5e1; border-left: none;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">كود الموظف:</span>
                                </td>
                                <td style=""padding: 10px 16px; font-family: monospace, Consolas, sans-serif; font-size: 16px; font-weight: 900; color: #065f46 !important; background-color: #ecfdf5; border-radius: 0 8px 8px 0; border: 1px solid #a7f3d0; border-right: none;"">
                                    <span style=""color: #065f46 !important; font-weight: 900;"">{employeeCode}</span>
                                </td>
                            </tr>
                            <!-- Row 2: Email -->
                            <tr>
                                <td style=""padding: 10px 14px; width: 145px; font-size: 14px; font-weight: 800; color: #1e293b !important; background-color: #f1f5f9; border-radius: 8px 0 0 8px; border: 1px solid #cbd5e1; border-left: none;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">البريد الإلكتروني:</span>
                                </td>
                                <td style=""padding: 10px 16px; font-family: monospace, Consolas, sans-serif; font-size: 14px; font-weight: 800; color: #0369a1 !important; background-color: #f0f9ff; border-radius: 0 8px 8px 0; border: 1px solid #bae6fd; border-right: none;"">
                                    <span style=""color: #0369a1 !important; font-weight: 800;"">{email}</span>
                                </td>
                            </tr>
                            <!-- Row 3: Password -->
                            <tr>
                                <td style=""padding: 12px 14px; width: 145px; font-size: 14px; font-weight: 800; color: #1e293b !important; background-color: #f1f5f9; border-radius: 8px 0 0 8px; border: 1px solid #cbd5e1; border-left: none;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">كلمة المرور المؤقتة:</span>
                                </td>
                                <td style=""padding: 12px 16px; font-family: monospace, Consolas, sans-serif; font-size: 18px; font-weight: 900; color: #b91c1c !important; background-color: #fef2f2; border-radius: 0 8px 8px 0; border: 1px solid #fecaca; border-right: none; letter-spacing: 1px;"">
                                    <span style=""color: #b91c1c !important; font-weight: 900;"">{initialCode}</span>
                                </td>
                            </tr>
                        </table>
                    </div>

                    <!-- Security Alert Callout -->
                    <div style=""background-color: #fffbeb; border: 1px solid #fde68a; border-right: 5px solid #d97706; border-radius: 10px; padding: 14px 18px; margin: 0 0 22px 0;"">
                        <div style=""font-size: 14px; font-weight: 900; color: #92400e !important; margin-bottom: 4px;"">
                            ⚠️ تنبيه أمني هام وإرشادات الدخول:
                        </div>
                        <p style=""font-size: 13px; color: #78350f !important; line-height: 1.6; margin: 0; font-weight: 600;"">
                            هذه كلمة مرور مؤقتة للاستخدام لمرة واحدة فقط. بمجرد تسجيل الدخول لأول مرة، سيطلب منك النظام مباشرةً تعيين كلمة مرور شخصية جديدة وتأكيدها.
                        </p>
                    </div>

                    <p style=""font-size: 13px; color: #64748b !important; line-height: 1.6; margin: 0; font-weight: 500;"">
                        للدخول للنظام، توجه لرابط المنظومة وأدخل البريد الإلكتروني وكلمة المرور المؤقتة المذكورة أعلاه.
                    </p>
                </div>

                <!-- Footer -->
                <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 18px 24px; text-align: center; color: #64748b; font-size: 12px;"">
                    <p style=""margin: 0 0 4px 0; font-weight: 700; color: #334155 !important;"">إدارة نظم المعلومات والتحول الرقمي • مطبعة جامعة أسيوط المركزية</p>
                    <p style=""margin: 0; color: #94a3b8 !important; font-size: 11px;"">تم إرسال هذا البريد تلقائياً من خادم التطبيق المركزي.</p>
                </div>
            </div>";

        return await TrySendSmtpEmailAsync(email, employeeName, subject, bodyHtml);
    }

    public async Task<(bool Success, string Message, string? Details)> TestSmtpConnectionAsync(string testRecipientEmail)
    {
        var host = _config["SmtpSettings:Host"];
        var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "ahta601@gmail.com";

        var subject = "مطبعة جامعة أسيوط - اختبار إرسال البريد الإلكتروني (SMTP Test)";
        var bodyHtml = $@"
            <div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, -apple-system, BlinkMacSystemFont, Roboto, sans-serif; max-width: 620px; margin: 20px auto; background-color: #ffffff; border: 1px solid #cbd5e1; border-radius: 18px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); color: #0f172a;"">
                <!-- Header Banner -->
                <div style=""background: linear-gradient(135deg, #064e3b 0%, #047857 50%, #0f766e 100%); padding: 28px 24px; text-align: center; color: #ffffff;"">
                    <div style=""display: inline-block; background-color: rgba(255,255,255,0.18); border: 1px solid rgba(255,255,255,0.3); border-radius: 9999px; padding: 5px 16px; margin-bottom: 10px; font-size: 12px; font-weight: bold; color: #ffffff;"">
                        جامعة أسيوط • جمهورية مصر العربية
                    </div>
                    <h1 style=""color: #ffffff !important; margin: 0; font-size: 22px; font-weight: 900;"">مطبعة جامعة أسيوط المركزية</h1>
                    <p style=""color: #a7f3d0 !important; font-size: 13px; margin: 6px 0 0 0; font-weight: 600;"">اختبار الاتصال بخادم البريد الإلكتروني (SMTP Test) ✅</p>
                </div>

                <!-- Body Content -->
                <div style=""padding: 30px 26px; background-color: #ffffff; color: #0f172a;"">
                    <p style=""font-size: 16px; color: #0f172a !important; margin: 0 0 12px 0; font-weight: 700;"">
                        تحية طيبة إلى مسؤول المنظومة،
                    </p>
                    <p style=""font-size: 14px; color: #334155 !important; line-height: 1.7; margin: 0 0 20px 0; font-weight: 500;"">
                        تم استلام هذه الرسالة بنجاح كاختبار مباشر لاتصال خادم البريد (SMTP) الخاص بمنظومة مطبعة جامعة أسيوط المركزية.
                    </p>

                    <!-- Technical Connection Specs Card -->
                    <div style=""background-color: #f8fafc; border: 2px solid #059669; border-radius: 14px; padding: 18px 20px; margin: 0 0 24px 0;"">
                        <div style=""font-size: 14px; font-weight: 900; color: #065f46 !important; margin-bottom: 12px;"">
                            ⚙️ تقرير مواصفات الاتصال الناجح:
                        </div>
                        <table style=""width: 100%; border-collapse: collapse;"">
                            <tr>
                                <td style=""padding: 8px 0; width: 130px; font-size: 13px; font-weight: 800; color: #1e293b !important;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">خادم البريد:</span>
                                </td>
                                <td style=""padding: 8px 0; font-family: monospace; font-size: 14px; font-weight: 800; color: #047857 !important;"">
                                    <span style=""color: #047857 !important; font-weight: 800;"">{host}</span>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 8px 0; width: 130px; font-size: 13px; font-weight: 800; color: #1e293b !important;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">البريد المرسل:</span>
                                </td>
                                <td style=""padding: 8px 0; font-family: monospace; font-size: 14px; font-weight: 800; color: #0284c7 !important;"">
                                    <span style=""color: #0284c7 !important; font-weight: 800;"">{senderEmail}</span>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 8px 0; width: 130px; font-size: 13px; font-weight: 800; color: #1e293b !important;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">المستلم التجريبي:</span>
                                </td>
                                <td style=""padding: 8px 0; font-family: monospace; font-size: 14px; font-weight: 800; color: #0f172a !important;"">
                                    <span style=""color: #0f172a !important; font-weight: 800;"">{testRecipientEmail}</span>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 8px 0; width: 130px; font-size: 13px; font-weight: 800; color: #1e293b !important;"">
                                    <span style=""color: #1e293b !important; font-weight: 800;"">توقيت الإرسال:</span>
                                </td>
                                <td style=""padding: 8px 0; font-family: monospace; font-size: 13px; font-weight: 700; color: #475569 !important;"">
                                    <span style=""color: #475569 !important; font-weight: 700;"">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</span>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>

                <!-- Footer -->
                <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 18px 24px; text-align: center; color: #64748b; font-size: 12px;"">
                    <p style=""margin: 0 0 4px 0; font-weight: 700; color: #334155 !important;"">إدارة نظم المعلومات والتحول الرقمي • مطبعة جامعة أسيوط المركزية</p>
                    <p style=""margin: 0; color: #94a3b8 !important; font-size: 11px;"">تم إرسال هذا البريد التجريبي تلقائياً من التطبيق المركزي.</p>
                </div>
            </div>";

        var (success, error) = await TrySendSmtpEmailAsync(testRecipientEmail, "مسؤول النظام", subject, bodyHtml);
        if (success)
        {
            return (true, $"تم إرسال بريد الاختبار بنجاح إلى ({testRecipientEmail}) عبر خادم ({host})", null);
        }
        else
        {
            return (false, "فشل إرسال بريد الاختبار عبر SMTP", error);
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> TrySendSmtpEmailAsync(string recipientEmail, string recipientName, string subject, string htmlContent)
    {
        try
        {
            var host = _config["SmtpSettings:Host"];
            var portStr = _config["SmtpSettings:Port"];
            var enableSsl = bool.TryParse(_config["SmtpSettings:EnableSsl"], out var ssl) && ssl;
            var rawUserName = _config["SmtpSettings:UserName"];
            var rawPassword = _config["SmtpSettings:Password"];
            var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "ahta601@gmail.com";
            var senderName = _config["SmtpSettings:SenderName"] ?? "مطبعة جامعة أسيوط المركزية";

            // Auto-Healing for Gmail and standard SMTP:
            // If UserName is empty or does not contain '@' (e.g., 'Ahmed_mohamed_refaat'),
            // automatically fallback to senderEmail ('ahta601@gmail.com') to ensure Gmail authentication succeeds!
            var userName = (!string.IsNullOrWhiteSpace(rawUserName) && rawUserName.Contains("@"))
                ? rawUserName.Trim()
                : senderEmail.Trim();

            // Auto-sanitizing: strip any spaces from Google App Password (e.g. 'bkyz ppec csjg uuze' -> 'bkyzppeccsjgouze')
            var password = rawPassword?.Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                var missing = $"Host: {(string.IsNullOrWhiteSpace(host) ? "مفقود" : "متوفر")}, UserName: {(string.IsNullOrWhiteSpace(userName) ? "مفقود" : "متوفر")}, Password: {(string.IsNullOrWhiteSpace(password) ? "مفقود" : "متوفر")}";
                _logger.LogWarning("[SMTP SKIPPED] Host, UserName or Password not configured properly: {Missing}", missing);
                return (false, $"إعدادات SMTP غير مكتملة: {missing}");
            }

            int port = 587;
            if (!string.IsNullOrWhiteSpace(portStr)) int.TryParse(portStr, out port);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password),
                Timeout = 15000
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
            _logger.LogInformation("[SMTP SUCCESS] Email successfully dispatched to {Email}", recipientEmail);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMTP FAILURE] Could not dispatch email via SMTP ({Message})", ex.Message);
            return (false, ex.Message);
        }
    }
}
