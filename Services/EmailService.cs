using System.Net;
using System.Net.Mail;

namespace OmniBizAI.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
    Task SendPasswordResetAsync(string toEmail, string resetUrl, string userName);
    Task SendWelcomeEmailAsync(string toEmail, string fullName, string loginUrl, string password);
}

/// <summary>
/// Email service with SMTP support.
/// Falls back to logging when SMTP is not configured (demo mode).
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"];
        var port = smtp.GetValue<int>("Port", 587);
        var user = smtp["UserName"];
        var pass = smtp["Password"];
        var from = smtp["FromEmail"] ?? "noreply@omnibiz.vn";
        var fromName = smtp["FromName"] ?? "OmniBizAI";

        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("SMTP not configured. Email to {Email} with subject '{Subject}' was NOT sent. Body logged for demo.", toEmail, subject);
            _logger.LogInformation("Email body: {Body}", htmlBody);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = smtp.GetValue<bool>("EnableSsl", true)
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetUrl, string userName)
    {
        var subject = "Đặt lại mật khẩu – OmniBizAI";
        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family:Inter,system-ui,-apple-system,sans-serif;background:#f5f5f7;margin:0;padding:40px 20px'>
<div style='max-width:480px;margin:0 auto;background:#fff;border-radius:16px;box-shadow:0 2px 12px rgba(0,0,0,.06);overflow:hidden'>
    <div style='background:linear-gradient(135deg,#0066cc,#5856d6);padding:32px 24px;text-align:center'>
        <div style='width:48px;height:48px;background:rgba(255,255,255,.2);border-radius:14px;display:inline-flex;align-items:center;justify-content:center;margin-bottom:12px'>
            <span style='font-size:22px;color:#fff'>🔑</span>
        </div>
        <h1 style='color:#fff;font-size:1.3rem;font-weight:700;margin:0'>Đặt lại mật khẩu</h1>
    </div>
    <div style='padding:28px 24px'>
        <p style='color:#1d1d1f;font-size:.92rem;margin:0 0 16px'>Xin chào <strong>{userName}</strong>,</p>
        <p style='color:#6e6e73;font-size:.88rem;line-height:1.6;margin:0 0 24px'>
            Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
            Nhấn vào nút bên dưới để tạo mật khẩu mới. Liên kết này có hiệu lực trong <strong>2 giờ</strong>.
        </p>
        <div style='text-align:center;margin-bottom:24px'>
            <a href='{resetUrl}' style='display:inline-block;background:#0066cc;color:#fff;padding:14px 36px;border-radius:12px;text-decoration:none;font-weight:600;font-size:.92rem;box-shadow:0 4px 12px rgba(0,102,204,.3)'>
                Đặt lại mật khẩu
            </a>
        </div>
        <p style='color:#86868b;font-size:.78rem;line-height:1.5;margin:0 0 16px'>
            Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
            Tài khoản của bạn vẫn an toàn.
        </p>
        <div style='background:#f5f5f7;border-radius:8px;padding:12px;margin-bottom:16px'>
            <p style='color:#86868b;font-size:.72rem;margin:0 0 4px'>Nếu nút không hoạt động, sao chép liên kết sau:</p>
            <p style='color:#0066cc;font-size:.7rem;word-break:break-all;margin:0'>{resetUrl}</p>
        </div>
    </div>
    <div style='border-top:1px solid #e8e8ed;padding:16px 24px;text-align:center'>
        <p style='color:#86868b;font-size:.7rem;margin:0'>© 2026 OmniBizAI — Hệ thống vận hành thông minh</p>
    </div>
</div>
</body>
</html>";

        await SendAsync(toEmail, subject, html);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string loginUrl, string password)
    {
        var subject = "Chào mừng bạn đến với OmniBizAI";
        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family:Inter,system-ui,-apple-system,sans-serif;background:#f5f5f7;margin:0;padding:40px 20px'>
<div style='max-width:480px;margin:0 auto;background:#fff;border-radius:16px;box-shadow:0 2px 12px rgba(0,0,0,.06);overflow:hidden'>
    <div style='background:linear-gradient(135deg,#30d158,#0066cc);padding:32px 24px;text-align:center'>
        <div style='width:48px;height:48px;background:rgba(255,255,255,.2);border-radius:14px;display:inline-flex;align-items:center;justify-content:center;margin-bottom:12px'>
            <span style='font-size:22px;color:#fff'>🎉</span>
        </div>
        <h1 style='color:#fff;font-size:1.3rem;font-weight:700;margin:0'>Chào mừng đến OmniBizAI!</h1>
    </div>
    <div style='padding:28px 24px'>
        <p style='color:#1d1d1f;font-size:.92rem;margin:0 0 16px'>Xin chào <strong>{fullName}</strong>,</p>
        <p style='color:#6e6e73;font-size:.88rem;line-height:1.6;margin:0 0 20px'>
            Tài khoản của bạn đã được tạo thành công trên hệ thống OmniBizAI.
            Dưới đây là thông tin đăng nhập của bạn:
        </p>
        <div style='background:#f5f5f7;border-radius:12px;padding:16px;margin-bottom:20px'>
            <div style='margin-bottom:8px'>
                <span style='font-size:.72rem;color:#86868b;text-transform:uppercase;letter-spacing:.5px'>Tên đăng nhập</span>
                <div style='font-size:.92rem;font-weight:600;color:#1d1d1f'>{toEmail}</div>
            </div>
            <div>
                <span style='font-size:.72rem;color:#86868b;text-transform:uppercase;letter-spacing:.5px'>Mật khẩu tạm thời</span>
                <div style='font-size:.92rem;font-weight:600;color:#1d1d1f;font-family:monospace'>{password}</div>
            </div>
        </div>
        <p style='color:#ff9500;font-size:.82rem;margin:0 0 20px'>
            ⚠️ Vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên.
        </p>
        <div style='text-align:center;margin-bottom:20px'>
            <a href='{loginUrl}' style='display:inline-block;background:#0066cc;color:#fff;padding:14px 36px;border-radius:12px;text-decoration:none;font-weight:600;font-size:.92rem;box-shadow:0 4px 12px rgba(0,102,204,.3)'>
                Đăng nhập ngay
            </a>
        </div>
    </div>
    <div style='border-top:1px solid #e8e8ed;padding:16px 24px;text-align:center'>
        <p style='color:#86868b;font-size:.7rem;margin:0'>© 2026 OmniBizAI — Hệ thống vận hành thông minh</p>
    </div>
</div>
</body>
</html>";

        await SendAsync(toEmail, subject, html);
    }
}
