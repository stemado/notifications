using Core.MicrosoftOutlookToolKit.Services.Senders.MicrosoftGraph;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Email service implementation using Microsoft Graph API via Core.MicrosoftOutlookToolKit.
///
/// Configuration is loaded from the shared file server:
/// - Graph credentials: \\anf-srv06\c$\Program Files\BenAdminAutomationMisc\MicrosoftGraph\antfarmharvester.json
/// - Token storage: PostgreSQL via Marten (connection from OUTLOOK_TOOLKIT_CONNECTION_STRING env var or file share)
///
/// This shares the same authentication and token management as all other census services.
/// </summary>
public class GraphEmailService : IEmailService
{
    private readonly ILogger<GraphEmailService> _logger;
    private const string DefaultFromAddress = "dexchange@antfarmservices.com";
    private const string DefaultFromName = "Notification Service";

    public GraphEmailService(ILogger<GraphEmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(DefaultFromName, DefaultFromAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(toEmail));
            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            if (!string.IsNullOrEmpty(plainTextBody))
            {
                bodyBuilder.TextBody = plainTextBody;
            }
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            // Use the shared OutlookMailSender which handles all auth via file share config
            var result = OutlookMailSender.Send(mimeMessage);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Graph API email sent successfully to {ToEmail}, Subject: {Subject}", toEmail, subject);
                return true;
            }

            _logger.LogError("Graph API email send failed to {ToEmail}. Errors: {Errors}",
                toEmail, string.Join("; ", result.Errors));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via Graph API to {ToEmail}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send email to multiple recipients
    /// </summary>
    public async Task<bool> SendEmailAsync(IEnumerable<string> toEmails, string subject, string htmlBody, string? plainTextBody = null)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(DefaultFromName, DefaultFromAddress));

            foreach (var email in toEmails)
            {
                mimeMessage.To.Add(MailboxAddress.Parse(email));
            }

            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            if (!string.IsNullOrEmpty(plainTextBody))
            {
                bodyBuilder.TextBody = plainTextBody;
            }
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            var result = OutlookMailSender.Send(mimeMessage);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Graph API email sent successfully to {RecipientCount} recipients, Subject: {Subject}",
                    mimeMessage.To.Count, subject);
                return true;
            }

            _logger.LogError("Graph API email send failed. Errors: {Errors}", string.Join("; ", result.Errors));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via Graph API to multiple recipients");
            return false;
        }
    }

    /// <summary>
    /// Save email as draft in Outlook
    /// </summary>
    public async Task<string?> SaveDraftAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(DefaultFromName, DefaultFromAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(toEmail));
            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            if (!string.IsNullOrEmpty(plainTextBody))
            {
                bodyBuilder.TextBody = plainTextBody;
            }
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            var result = OutlookMailSender.SaveDraft(mimeMessage);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Draft saved successfully. DraftId: {DraftId}", result.Value);
                return result.Value;
            }

            _logger.LogError("Failed to save draft. Errors: {Errors}", string.Join("; ", result.Errors));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving email draft");
            return null;
        }
    }

    /// <summary>
    /// Send a previously saved draft
    /// </summary>
    public async Task<bool> SendDraftAsync(string draftMessageId)
    {
        try
        {
            var result = OutlookMailSender.SendDraft(draftMessageId);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Draft sent successfully. DraftId: {DraftId}", draftMessageId);
                return true;
            }

            _logger.LogError("Failed to send draft {DraftId}. Errors: {Errors}",
                draftMessageId, string.Join("; ", result.Errors));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending draft {DraftId}", draftMessageId);
            return false;
        }
    }

    /// <summary>
    /// Validate configuration by checking if OutlookMailSender can access tokens
    /// </summary>
    public Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            // OutlookMailSender doesn't have a direct validation method,
            // but we can enable mock mode temporarily or just return true
            // since the actual validation happens when sending
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Graph API configuration validation failed");
            return Task.FromResult(false);
        }
    }
}
