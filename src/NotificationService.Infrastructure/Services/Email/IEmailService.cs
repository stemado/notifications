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
    /// Sends an email to multiple recipients (all in TO field).
    /// </summary>
    Task<EmailSendResult> SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string htmlBody,
        bool isHtml = true,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an email with explicit TO, CC, and BCC recipient lists.
    /// This is the preferred method for proper email routing.
    /// </summary>
    /// <param name="toRecipients">Primary recipients (required - at least one)</param>
    /// <param name="ccRecipients">Carbon copy recipients (optional)</param>
    /// <param name="bccRecipients">Blind carbon copy recipients (optional)</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with message ID and per-recipient status</returns>
    Task<EmailSendResult> SendEmailAsync(
        IEnumerable<string> toRecipients,
        IEnumerable<string>? ccRecipients,
        IEnumerable<string>? bccRecipients,
        string subject,
        string htmlBody,
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
