namespace NotificationService.Domain.DTOs;

/// <summary>
/// Base class for all channel configurations
/// </summary>
public abstract class ChannelConfigurationBase
{
    public bool Enabled { get; set; }
}

/// <summary>
/// Email channel configuration (SMTP or Microsoft Graph)
/// </summary>
public class EmailChannelConfiguration : ChannelConfigurationBase
{
    public string Provider { get; set; } = "graph"; // "smtp" or "graph"
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string? ReplyToAddress { get; set; }
    public bool EnableSsl { get; set; } = true;
}

/// <summary>
/// SMS channel configuration (Twilio)
/// </summary>
public class SmsChannelConfiguration : ChannelConfigurationBase
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Microsoft Teams channel configuration
/// </summary>
public class TeamsChannelConfiguration : ChannelConfigurationBase
{
    public string WebhookUrl { get; set; } = string.Empty;
    public string? ChannelName { get; set; }
}

/// <summary>
/// SignalR channel configuration (read-only status)
/// </summary>
public class SignalRChannelConfiguration : ChannelConfigurationBase
{
    public string HubUrl { get; set; } = "/hubs/notifications";
    public bool AutoReconnect { get; set; } = true;
}

/// <summary>
/// Request to update a channel configuration
/// </summary>
public class UpdateChannelConfigurationRequest
{
    public bool Enabled { get; set; }
    public string? Provider { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? FromAddress { get; set; }
    public string? ReplyToAddress { get; set; }
    public bool? EnableSsl { get; set; }
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? FromPhoneNumber { get; set; }
    public string? WebhookUrl { get; set; }
    public string? ChannelName { get; set; }
}

/// <summary>
/// Response containing channel configuration (with masked secrets)
/// </summary>
public class ChannelConfigurationResponse
{
    public string Channel { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool Configured { get; set; }
    public object? Config { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public string? TestStatus { get; set; }
    public string? TestError { get; set; }
}

/// <summary>
/// Result of testing a channel connection
/// </summary>
public class TestChannelResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
