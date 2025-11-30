namespace NotificationService.Client.Configuration;

/// <summary>
/// Configuration options for the NotificationService client.
/// </summary>
public class NotificationServiceOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "NotificationService";

    /// <summary>
    /// Base URL of the NotificationService API (e.g., "http://anf-srv06.antfarmllc.local:5201")
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds. Default: 30
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed requests. Default: 3
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for retry backoff. Default: 1000 (1 second)
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Number of failures before circuit breaker opens. Default: 5
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration in seconds to keep circuit breaker open. Default: 60
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Sampling duration in seconds for circuit breaker. Default: 30
    /// </summary>
    public int CircuitBreakerSamplingSeconds { get; set; } = 30;

    /// <summary>
    /// Optional API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException(
                $"NotificationService BaseUrl is required. Configure '{SectionName}:BaseUrl' in appsettings.json.");
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"NotificationService BaseUrl '{BaseUrl}' is not a valid URI.");
        }
    }
}
