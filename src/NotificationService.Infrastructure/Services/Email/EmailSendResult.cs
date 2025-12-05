namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Result of an email send operation with detailed error information.
/// </summary>
public class EmailSendResult
{
    public bool Success { get; private set; }
    public string? MessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }
    public EmailProvider Provider { get; private set; }
    public DateTime SentAt { get; private set; }

    private EmailSendResult() { }

    public static EmailSendResult Successful(string? messageId, EmailProvider provider)
    {
        return new EmailSendResult
        {
            Success = true,
            MessageId = messageId,
            Provider = provider,
            SentAt = DateTime.UtcNow
        };
    }

    public static EmailSendResult Failed(string errorMessage, EmailProvider provider, Exception? exception = null)
    {
        return new EmailSendResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Provider = provider,
            SentAt = DateTime.UtcNow
        };
    }

    public static EmailSendResult ConfigurationError(string errorMessage, EmailProvider provider)
    {
        return new EmailSendResult
        {
            Success = false,
            ErrorMessage = $"Configuration error: {errorMessage}",
            Provider = provider,
            SentAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Available email providers.
/// </summary>
public enum EmailProvider
{
    /// <summary>SMTP provider (for Papercut in development or production SMTP)</summary>
    Smtp,

    /// <summary>Microsoft Graph API provider (delegated user via OAuth)</summary>
    MicrosoftGraph,

    /// <summary>No provider configured or provider selection failed</summary>
    None
}
