using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DataSenseAPI.Infrastructure.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task QueueEmailAsync(string to, string subject, string htmlBody);
}

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailQueue _emailQueue;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration, IEmailQueue emailQueue)
    {
        _logger = logger;
        _configuration = configuration;
        _emailQueue = emailQueue;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpPort = int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 587;
        var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@datasense.local";
        var fromName = _configuration["Smtp:FromName"] ?? "DataSense";

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("[DEV MODE] No SMTP configured. Email logged instead. To: {To} Subject: {Subject}", to, subject);
            _logger.LogInformation("[DEV EMAIL] To: {To}\nSubject: {Subject}\n{Body}", to, subject, htmlBody);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To} via SMTP", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public Task QueueEmailAsync(string to, string subject, string htmlBody)
    {
        _emailQueue.Enqueue(new EmailMessage
        {
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            QueuedAt = DateTime.UtcNow
        });
        _logger.LogDebug("Email queued for {To}", to);
        return Task.CompletedTask;
    }
}


