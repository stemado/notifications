using Core.MicrosoftOutlookToolKit.Services.Senders.MicrosoftGraph;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly MicrosoftGraphOptions _options;

    public GraphEmailService(ILogger<GraphEmailService> logger, IOptions<EmailProviderOptions> options)
    {
        _logger = logger;
        _options = options.Value.MicrosoftGraph;
    }

    public EmailProvider CurrentProvider => EmailProvider.MicrosoftGraph;

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

        try
        {
            var fromAddress = !string.IsNullOrEmpty(_options.SendFromAddress)
                ? _options.SendFromAddress
                : "dexchange@antfarmservices.com";

            var fromName = "Notification Service";

            _logger.LogInformation(
                "Sending email via Microsoft Graph API from {From} to {RecipientCount} recipients. Subject: {Subject}",
                fromAddress,
                recipientList.Count,
                subject);

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(fromName, fromAddress));

            foreach (var email in recipientList)
            {
                mimeMessage.To.Add(MailboxAddress.Parse(email));
            }

            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = htmlBody;
            }
            else
            {
                bodyBuilder.TextBody = htmlBody;
            }
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            // Use the shared OutlookMailSender which handles all auth via file share config
            var result = OutlookMailSender.Send(mimeMessage);

            if (result.IsSuccess)
            {
                var messageId = Guid.NewGuid().ToString();
                _logger.LogInformation(
                    "Email sent successfully via Graph API. MessageId: {MessageId}, Recipients: {Recipients}",
                    messageId,
                    string.Join(", ", recipientList));

                return EmailSendResult.Successful(messageId, EmailProvider.MicrosoftGraph);
            }

            var errorMessage = $"Graph API send failed: {string.Join("; ", result.Errors.Select(e => e.Message))}";
            _logger.LogError(
                "Graph API email send failed to {Recipients}. Errors: {Errors}",
                string.Join(", ", recipientList),
                errorMessage);

            return EmailSendResult.Failed(errorMessage, EmailProvider.MicrosoftGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending email via Graph API to {Recipients}", string.Join(", ", recipientList));
            return EmailSendResult.Failed($"Graph API exception: {ex.Message}", EmailProvider.MicrosoftGraph, ex);
        }
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            // OutlookMailSender loads config from file share automatically
            // Validation happens when sending - we just check basic setup
            _logger.LogDebug("Graph API configuration validation - delegating to OutlookMailSender at runtime");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Graph API configuration validation failed");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Save email as draft in Outlook (useful for manual review before sending).
    /// </summary>
    public async Task<EmailSendResult> SaveDraftAsync(
        IEnumerable<string> recipients,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        var recipientList = recipients.ToList();

        try
        {
            var fromAddress = !string.IsNullOrEmpty(_options.SendFromAddress)
                ? _options.SendFromAddress
                : "dexchange@antfarmservices.com";

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress("Notification Service", fromAddress));

            foreach (var email in recipientList)
            {
                mimeMessage.To.Add(MailboxAddress.Parse(email));
            }

            mimeMessage.Subject = subject;
            mimeMessage.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            var result = OutlookMailSender.SaveDraft(mimeMessage);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Draft saved successfully. DraftId: {DraftId}", result.Value);
                return EmailSendResult.Successful(result.Value, EmailProvider.MicrosoftGraph);
            }

            var errorMessage = $"Failed to save draft: {string.Join("; ", result.Errors.Select(e => e.Message))}";
            _logger.LogError("Failed to save draft. Errors: {Errors}", errorMessage);
            return EmailSendResult.Failed(errorMessage, EmailProvider.MicrosoftGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving email draft");
            return EmailSendResult.Failed($"Failed to save draft: {ex.Message}", EmailProvider.MicrosoftGraph, ex);
        }
    }
}
