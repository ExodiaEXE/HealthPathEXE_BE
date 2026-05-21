using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;
using HealthPath.API.Models;

namespace HealthPath.API.Services.Channels;

public class EmailChannel : INotificationChannel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailChannel> _logger;

    public EmailChannel(IConfiguration configuration, ILogger<EmailChannel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string Name => "email";

    public async Task SendAsync(Notification notification, User user)
    {
        var host = _configuration["Smtp:Host"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("SMTP Configuration is incomplete. Skipping Email Notification.");
            return;
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("User {UserId} has no email address. Skipping Email.", user.Id);
            return;
        }

        try
        {
            var emailMessage = new MimeMessage();
            var fromName = _configuration["Smtp:FromName"] ?? "HealthPath";
            var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@healthpath.vn";

            emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));
            emailMessage.To.Add(new MailboxAddress(user.FullName ?? user.Email, user.Email));
            emailMessage.Subject = notification.Title;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2>{notification.Title}</h2>
                        <p>{notification.Body}</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                        <small style='color: #888;'>HealthPath — Ứng dụng Quản lý Lối sống Lành mạnh</small>
                    </div>"
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var portStr = _configuration["Smtp:Port"];
            int port = int.TryParse(portStr, out int p) ? p : 587;

            // Connect using appropriate SecureSocketOptions
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", user.Email);
        }
    }
}
