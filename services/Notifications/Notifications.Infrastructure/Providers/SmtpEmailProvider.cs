using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Notifications.Application.Interfaces;
using Notifications.Domain.Enums;
using Notifications.Domain.Models;
using ExamPrep.Shared.Constants;

namespace Notifications.Infrastructure.Providers;

public class SmtpEmailProvider : INotificationProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(IConfiguration config, ILogger<SmtpEmailProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public NotificationType Type => NotificationType.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailMessage = new MimeMessage();
            var fromAddress = _config[ConfigKeys.Email.FromAddress] ?? throw new InvalidOperationException("Email:FromAddress is not configured.");
            emailMessage.From.Add(new MailboxAddress($"{AppConstants.AppName} System", fromAddress));
            emailMessage.To.Add(new MailboxAddress("", message.Recipient));
            emailMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = message.Body };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // For Google SMTP
            var host = _config[ConfigKeys.Email.SmtpHost] ?? "smtp.gmail.com";
            var port = int.Parse(_config[ConfigKeys.Email.SmtpPort] ?? "587");
            var username = _config[ConfigKeys.Email.Username] ?? throw new InvalidOperationException("Email:Username is not configured.");
            var password = _config[ConfigKeys.Email.Password] ?? throw new InvalidOperationException("Email:Password is not configured."); // Use App Password for Gmail

            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(emailMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Successfully sent email to {Recipient} with subject '{Subject}'", message.Recipient, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.Recipient);
            throw;
        }
    }
}
