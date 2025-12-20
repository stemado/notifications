using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Email service implementation using SMTP.
/// Supports both local development servers (Papercut) and production SMTP servers.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly SmtpOptions _options;

    public SmtpEmailService(ILogger<SmtpEmailService> logger, IOptions<EmailProviderOptions> options)
    {
        _logger = logger;
        _options = options.Value.Smtp;
    }

    public EmailProvider CurrentProvider => EmailProvider.Smtp;

    public async Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
    {
        return await SendEmailAsync(new[] { toEmail }, subject, htmlBody, true, ct);
    }

    public async Task<EmailSendResult> SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string htmlBody,
        bool isHtml = true,
        CancellationToken ct = default)
    {
        var recipientList = recipients.ToList();

        // Validate configuration
        if (string.IsNullOrEmpty(_options.Host))
        {
            _logger.LogError("SMTP Host is not configured");
            return EmailSendResult.ConfigurationError("SMTP Host is not configured", EmailProvider.Smtp);
        }

        if (string.IsNullOrEmpty(_options.FromEmail))
        {
            _logger.LogError("SMTP FromEmail is not configured");
            return EmailSendResult.ConfigurationError("From email address is not configured", EmailProvider.Smtp);
        }

        // For non-dev servers, require credentials
        if (!_options.IsLocalDevServer && string.IsNullOrEmpty(_options.Username))
        {
            _logger.LogError("SMTP credentials required for production servers");
            return EmailSendResult.ConfigurationError("SMTP credentials are required for production servers", EmailProvider.Smtp);
        }

        try
        {
            _logger.LogInformation(
                "Sending email via SMTP ({Host}:{Port}) to {RecipientCount} recipients. Subject: {Subject}",
                _options.Host,
                _options.Port,
                recipientList.Count,
                subject);

            using var smtpClient = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(recipientList, subject, htmlBody, isHtml);

            await smtpClient.SendMailAsync(mailMessage, ct);

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation(
                "Email sent successfully via SMTP. MessageId: {MessageId}, Recipients: {Recipients}",
                messageId,
                string.Join(", ", recipientList));

            return EmailSendResult.Successful(messageId, EmailProvider.Smtp);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Recipients}. Status: {StatusCode}",
                string.Join(", ", recipientList), ex.StatusCode);
            return EmailSendResult.Failed($"SMTP error: {ex.Message} (Status: {ex.StatusCode})", EmailProvider.Smtp, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Recipients}", string.Join(", ", recipientList));
            return EmailSendResult.Failed($"Failed to send email: {ex.Message}", EmailProvider.Smtp, ex);
        }
    }

    public async Task<EmailSendResult> SendEmailAsync(
        IEnumerable<string> toRecipients,
        IEnumerable<string>? ccRecipients,
        IEnumerable<string>? bccRecipients,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        var toList = toRecipients.ToList();
        var ccList = ccRecipients?.ToList() ?? [];
        var bccList = bccRecipients?.ToList() ?? [];

        if (toList.Count == 0)
        {
            return EmailSendResult.Failed("At least one TO recipient is required", EmailProvider.Smtp);
        }

        // Validate configuration
        if (string.IsNullOrEmpty(_options.Host))
        {
            _logger.LogError("SMTP Host is not configured");
            return EmailSendResult.ConfigurationError("SMTP Host is not configured", EmailProvider.Smtp);
        }

        if (string.IsNullOrEmpty(_options.FromEmail))
        {
            _logger.LogError("SMTP FromEmail is not configured");
            return EmailSendResult.ConfigurationError("From email address is not configured", EmailProvider.Smtp);
        }

        if (!_options.IsLocalDevServer && string.IsNullOrEmpty(_options.Username))
        {
            _logger.LogError("SMTP credentials required for production servers");
            return EmailSendResult.ConfigurationError("SMTP credentials are required for production servers", EmailProvider.Smtp);
        }

        try
        {
            _logger.LogInformation(
                "Sending email via SMTP ({Host}:{Port}). TO: {ToCount}, CC: {CcCount}, BCC: {BccCount}. Subject: {Subject}",
                _options.Host,
                _options.Port,
                toList.Count,
                ccList.Count,
                bccList.Count,
                subject);

            using var smtpClient = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(toList, ccList, bccList, subject, htmlBody, isHtml: true);

            await smtpClient.SendMailAsync(mailMessage, ct);

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation(
                "Email sent successfully via SMTP. MessageId: {MessageId}, TO: {To}, CC: {Cc}, BCC: {Bcc}",
                messageId,
                string.Join(", ", toList),
                string.Join(", ", ccList),
                string.Join(", ", bccList));

            return EmailSendResult.Successful(messageId, EmailProvider.Smtp);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email. TO: {To}. Status: {StatusCode}",
                string.Join(", ", toList), ex.StatusCode);
            return EmailSendResult.Failed($"SMTP error: {ex.Message} (Status: {ex.StatusCode})", EmailProvider.Smtp, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email. TO: {To}", string.Join(", ", toList));
            return EmailSendResult.Failed($"Failed to send email: {ex.Message}", EmailProvider.Smtp, ex);
        }
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken ct = default)
    {
        var isValid = !string.IsNullOrEmpty(_options.Host) &&
                      !string.IsNullOrEmpty(_options.FromEmail) &&
                      (_options.IsLocalDevServer || !string.IsNullOrEmpty(_options.Username));

        if (!isValid)
        {
            _logger.LogWarning(
                "SMTP configuration validation failed. Host: {Host}, FromEmail: {FromEmail}, IsLocalDev: {IsLocal}, HasCredentials: {HasCreds}",
                _options.Host ?? "(not set)",
                _options.FromEmail ?? "(not set)",
                _options.IsLocalDevServer,
                !string.IsNullOrEmpty(_options.Username));
        }

        return Task.FromResult(isValid);
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // For local dev servers like Papercut, no credentials needed
        if (!_options.IsLocalDevServer && !string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private MailMessage CreateMailMessage(List<string> recipients, string subject, string body, bool isHtml)
    {
        return CreateMailMessage(recipients, [], [], subject, body, isHtml);
    }

    private MailMessage CreateMailMessage(
        List<string> toRecipients,
        List<string> ccRecipients,
        List<string> bccRecipients,
        string subject,
        string body,
        bool isHtml)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        foreach (var recipient in toRecipients)
        {
            mailMessage.To.Add(recipient);
        }

        foreach (var recipient in ccRecipients)
        {
            mailMessage.CC.Add(recipient);
        }

        foreach (var recipient in bccRecipients)
        {
            mailMessage.Bcc.Add(recipient);
        }

        return mailMessage;
    }
}
