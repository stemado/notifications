namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    Task<bool> SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string htmlBody,
        bool isHtml = true,
        CancellationToken ct = default);
}
