using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using OmniBizAI.Services.Options;

namespace OmniBizAI.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> smtpOptions, ILogger<EmailService> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrEmpty(_smtpOptions.Host)) 
        {
            _logger.LogWarning("SMTP Host is not configured. Simulating email send to {Email}", toEmail);
            return;
        }

        using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
        {
            Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_smtpOptions.FromEmail ?? "no-reply@omnibiz.ai", _smtpOptions.FromName ?? "OmniBiz AI"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        
        mailMessage.To.Add(toEmail);

        try 
        {
            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
