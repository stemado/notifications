namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);
}
