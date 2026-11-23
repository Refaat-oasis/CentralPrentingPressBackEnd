using System.Threading.Tasks;

namespace CentralPress.API.Services;

public interface IEmailRetryJob
{
    Task SendEmailWithRetryAsync(string recipientEmail, string recipientName, string subject, string bodyHtml);
}
