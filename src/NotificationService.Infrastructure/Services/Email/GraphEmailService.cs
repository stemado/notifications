using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Email service implementation using Microsoft Graph API.
/// 
/// NOTE: This is a stub implementation. The Core.MicrosoftOutlookToolKit dependency
/// is not currently available. Use SmtpEmailService instead for email delivery.
/// 
/// To enable Graph API support:
/// 1. Add Core.MicrosoftOutlookToolKit project reference
/// 2. Uncomment the Graph API implementation
/// </summary>
public class GraphEmailService : IEmailService
{
    private readonly ILogger<GraphEmailService> _logger;

    public GraphEmailService(ILogger<GraphEmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        _logger.LogWarning(
            "GraphEmailService is not configured. Core.MicrosoftOutlookToolKit dependency is missing. " +
            "Email to {ToEmail} was not sent. Use SmtpEmailService instead.", toEmail);

        // Return false to indicate email was not sent - caller should fall back to SMTP
        return Task.FromResult(false);
    }

    public Task<bool> SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string htmlBody,
        bool isHtml = true,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "GraphEmailService is not configured. Core.MicrosoftOutlookToolKit dependency is missing. " +
            "Email to {RecipientCount} recipients was not sent. Use SmtpEmailService instead.",
            recipients.Count());

        // Return false to indicate email was not sent - caller should fall back to SMTP
        return Task.FromResult(false);
    }

    /// <summary>
    /// Validate configuration - returns false since Graph API is not available
    /// </summary>
    public Task<bool> ValidateConfigurationAsync()
    {
        _logger.LogDebug("GraphEmailService.ValidateConfigurationAsync: Graph API not available (Core.MicrosoftOutlookToolKit missing)");
        return Task.FromResult(false);
    }
}
