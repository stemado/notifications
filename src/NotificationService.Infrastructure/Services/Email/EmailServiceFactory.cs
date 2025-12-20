using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Factory for creating the appropriate email service based on configuration.
///
/// Provider selection:
/// - "Smtp" : Uses SmtpEmailService (Papercut for dev, production SMTP later)
/// - "MicrosoftGraph" : Uses GraphEmailService (delegated user via OAuth)
/// </summary>
public interface IEmailServiceFactory
{
    /// <summary>
    /// Gets the configured email service.
    /// </summary>
    IEmailService GetEmailService();

    /// <summary>
    /// Gets the currently configured provider type.
    /// </summary>
    EmailProvider ConfiguredProvider { get; }
}

/// <summary>
/// Implementation of email service factory.
/// </summary>
public class EmailServiceFactory : IEmailServiceFactory
{
    private readonly IEmailService _emailService;
    private readonly EmailProvider _configuredProvider;
    private readonly ILogger<EmailServiceFactory> _logger;

    public EmailServiceFactory(
        IOptions<EmailProviderOptions> options,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EmailServiceFactory>();
        var providerOptions = options.Value;

        // Parse provider from configuration
        _configuredProvider = ParseProvider(providerOptions.Provider);

        // Create the appropriate service
        _emailService = _configuredProvider switch
        {
            EmailProvider.MicrosoftGraph => new GraphEmailService(
                loggerFactory.CreateLogger<GraphEmailService>(),
                options),

            EmailProvider.Smtp => new SmtpEmailService(
                loggerFactory.CreateLogger<SmtpEmailService>(),
                options),

            _ => throw new InvalidOperationException($"Unknown email provider: {providerOptions.Provider}")
        };

        _logger.LogInformation(
            "Email service factory initialized. Provider: {Provider}, Host: {Host}",
            _configuredProvider,
            _configuredProvider == EmailProvider.Smtp
                ? $"{providerOptions.Smtp.Host}:{providerOptions.Smtp.Port}"
                : "Microsoft Graph API");
    }

    public EmailProvider ConfiguredProvider => _configuredProvider;

    public IEmailService GetEmailService() => _emailService;

    private static EmailProvider ParseProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return EmailProvider.Smtp; // Default

        return provider.ToLowerInvariant() switch
        {
            "smtp" => EmailProvider.Smtp,
            "microsoftgraph" or "graph" or "msgraph" => EmailProvider.MicrosoftGraph,
            _ => throw new InvalidOperationException($"Unknown email provider: {provider}. Valid options: Smtp, MicrosoftGraph")
        };
    }
}

/// <summary>
/// Wrapper email service that delegates to the factory-selected provider.
/// This is the service registered in DI as IEmailService.
/// </summary>
public class FactoryBasedEmailService : IEmailService
{
    private readonly IEmailService _innerService;

    public FactoryBasedEmailService(IEmailServiceFactory factory)
    {
        _innerService = factory.GetEmailService();
    }

    public EmailProvider CurrentProvider => _innerService.CurrentProvider;

    public Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
        => _innerService.SendEmailAsync(toEmail, subject, htmlBody, plainTextBody, ct);

    public Task<EmailSendResult> SendEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody, bool isHtml = true, CancellationToken ct = default)
        => _innerService.SendEmailAsync(recipients, subject, htmlBody, isHtml, ct);

    public Task<EmailSendResult> SendEmailAsync(
        IEnumerable<string> toRecipients,
        IEnumerable<string>? ccRecipients,
        IEnumerable<string>? bccRecipients,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
        => _innerService.SendEmailAsync(toRecipients, ccRecipients, bccRecipients, subject, htmlBody, ct);

    public Task<bool> ValidateConfigurationAsync(CancellationToken ct = default)
        => _innerService.ValidateConfigurationAsync(ct);
}
