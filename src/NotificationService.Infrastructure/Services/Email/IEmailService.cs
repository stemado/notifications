namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Service for sending emails with detailed result information.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default);

    /// <summary>
    /// Sends an email to multiple recipients.
    /// </summary>
    Task<EmailSendResult> SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string htmlBody,
        bool isHtml = true,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current email provider type.
    /// </summary>
    EmailProvider CurrentProvider { get; }

    /// <summary>
    /// Validates that the email service is configured properly.
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken ct = default);
}
