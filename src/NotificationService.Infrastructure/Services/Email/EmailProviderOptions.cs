namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Configuration options for email provider selection.
/// </summary>
public class EmailProviderOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// The email provider to use: "Smtp" or "MicrosoftGraph"
    /// </summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>
    /// SMTP configuration (for Papercut in development or production SMTP)
    /// </summary>
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>
    /// Microsoft Graph API configuration
    /// </summary>
    public MicrosoftGraphOptions MicrosoftGraph { get; set; } = new();
}

/// <summary>
/// SMTP server configuration options.
/// </summary>
public class SmtpOptions
{
    /// <summary>SMTP server host (e.g., "localhost" for Papercut, "smtp.gmail.com" for Gmail)</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>SMTP server port (25 for Papercut, 587 for TLS, 465 for SSL)</summary>
    public int Port { get; set; } = 25;

    /// <summary>Username for SMTP authentication (leave empty for Papercut)</summary>
    public string? Username { get; set; }

    /// <summary>Password for SMTP authentication</summary>
    public string? Password { get; set; }

    /// <summary>Enable SSL/TLS</summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>From email address</summary>
    public string FromEmail { get; set; } = "noreply@localhost";

    /// <summary>From display name</summary>
    public string FromName { get; set; } = "Notification Service";

    /// <summary>Whether this is a local development server like Papercut (skips authentication)</summary>
    public bool IsLocalDevServer { get; set; } = true;
}

/// <summary>
/// Microsoft Graph API configuration options for delegated user email.
/// </summary>
public class MicrosoftGraphOptions
{
    /// <summary>Azure AD Tenant ID</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Azure AD Application (Client) ID</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Azure AD Client Secret</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Email address to send from (must have delegated permissions)</summary>
    public string SendFromAddress { get; set; } = string.Empty;

    /// <summary>Optional: Scopes for Graph API access</summary>
    public string[] Scopes { get; set; } = new[] { "https://graph.microsoft.com/.default" };
}
