using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Enhanced email service with primary/fallback support.
/// Uses Graph API (via Core.MicrosoftOutlookToolKit) as primary with SMTP fallback.
///
/// Configuration:
/// - Graph: Uses shared file server config (\\anf-srv06\c$\Program Files\BenAdminAutomationMisc\MicrosoftGraph\antfarmharvester.json)
/// - SMTP: Uses appsettings.json Email:SmtpHost, Email:SmtpPort, etc.
/// - Mode: Set via Email:Mode in appsettings.json (GraphOnly, SmtpOnly, GraphWithSmtpFallback, SmtpWithGraphFallback)
/// </summary>
public class EnhancedEmailService : IEmailService
{
    private readonly ILogger<EnhancedEmailService> _logger;
    private readonly GraphEmailService? _graphService;
    private readonly SmtpEmailService? _smtpService;
    private readonly EmailServiceMode _mode;

    public enum EmailServiceMode
    {
        GraphOnly,
        SmtpOnly,
        GraphWithSmtpFallback,
        SmtpWithGraphFallback
    }

    public EnhancedEmailService(
        ILogger<EnhancedEmailService> logger,
        IConfiguration configuration,
        ILogger<GraphEmailService> graphLogger,
        ILogger<SmtpEmailService> smtpLogger)
    {
        _logger = logger;

        // Parse mode from configuration
        var modeConfig = configuration["Email:Mode"] ?? "GraphWithSmtpFallback";
        _mode = Enum.TryParse<EmailServiceMode>(modeConfig, true, out var parsedMode)
            ? parsedMode
            : EmailServiceMode.GraphWithSmtpFallback;

        _logger.LogInformation("EnhancedEmailService initialized with mode: {Mode}", _mode);

        // Initialize Graph service (uses file share config via Core.MicrosoftOutlookToolKit)
        try
        {
            if (_mode != EmailServiceMode.SmtpOnly)
            {
                _graphService = new GraphEmailService(graphLogger);
                _logger.LogInformation("Graph email service initialized (using file share config)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Graph email service");
            if (_mode == EmailServiceMode.GraphOnly)
            {
                throw;
            }
        }

        // Initialize SMTP service (uses appsettings.json)
        try
        {
            if (_mode != EmailServiceMode.GraphOnly)
            {
                _smtpService = new SmtpEmailService(smtpLogger, configuration);
                _logger.LogInformation("SMTP email service initialized");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize SMTP email service");
            if (_mode == EmailServiceMode.SmtpOnly)
            {
                throw;
            }
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        return _mode switch
        {
            EmailServiceMode.GraphOnly => await SendViaGraphAsync(toEmail, subject, htmlBody, plainTextBody),
            EmailServiceMode.SmtpOnly => await SendViaSmtpAsync(toEmail, subject, htmlBody, plainTextBody),
            EmailServiceMode.GraphWithSmtpFallback => await SendWithFallbackAsync(
                () => SendViaGraphAsync(toEmail, subject, htmlBody, plainTextBody),
                () => SendViaSmtpAsync(toEmail, subject, htmlBody, plainTextBody),
                "Graph", "SMTP"),
            EmailServiceMode.SmtpWithGraphFallback => await SendWithFallbackAsync(
                () => SendViaSmtpAsync(toEmail, subject, htmlBody, plainTextBody),
                () => SendViaGraphAsync(toEmail, subject, htmlBody, plainTextBody),
                "SMTP", "Graph"),
            _ => await SendViaGraphAsync(toEmail, subject, htmlBody, plainTextBody)
        };
    }

    private async Task<bool> SendViaGraphAsync(string toEmail, string subject, string htmlBody, string? plainTextBody)
    {
        if (_graphService == null)
        {
            _logger.LogWarning("Graph service not available");
            return false;
        }

        return await _graphService.SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
    }

    private async Task<bool> SendViaSmtpAsync(string toEmail, string subject, string htmlBody, string? plainTextBody)
    {
        if (_smtpService == null)
        {
            _logger.LogWarning("SMTP service not available");
            return false;
        }

        return await _smtpService.SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
    }

    private async Task<bool> SendWithFallbackAsync(
        Func<Task<bool>> primarySend,
        Func<Task<bool>> fallbackSend,
        string primaryName,
        string fallbackName)
    {
        try
        {
            var success = await primarySend();
            if (success)
            {
                return true;
            }

            _logger.LogWarning("{Primary} email send failed, attempting {Fallback} fallback", primaryName, fallbackName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Primary} email send threw exception, attempting {Fallback} fallback",
                primaryName, fallbackName);
        }

        try
        {
            var fallbackSuccess = await fallbackSend();
            if (fallbackSuccess)
            {
                _logger.LogInformation("Email sent successfully via {Fallback} fallback", fallbackName);
            }
            return fallbackSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback {Fallback} email send also failed", fallbackName);
            return false;
        }
    }

    /// <summary>
    /// Get the current health status of configured email services
    /// </summary>
    public async Task<EmailHealthStatus> GetHealthStatusAsync()
    {
        var status = new EmailHealthStatus
        {
            Mode = _mode.ToString(),
            GraphAvailable = _graphService != null,
            SmtpAvailable = _smtpService != null
        };

        if (_graphService != null)
        {
            try
            {
                status.GraphHealthy = await _graphService.ValidateConfigurationAsync();
            }
            catch
            {
                status.GraphHealthy = false;
            }
        }

        // SMTP health - basic check
        status.SmtpHealthy = _smtpService != null;

        return status;
    }
}

public class EmailHealthStatus
{
    public string Mode { get; set; } = string.Empty;
    public bool GraphAvailable { get; set; }
    public bool GraphHealthy { get; set; }
    public bool SmtpAvailable { get; set; }
    public bool SmtpHealthy { get; set; }

    public bool IsHealthy => (GraphAvailable && GraphHealthy) || (SmtpAvailable && SmtpHealthy);
}
