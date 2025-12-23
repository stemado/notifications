namespace NotificationService.Client.Routing;

/// <summary>
/// Configuration options for the routing service client.
/// </summary>
public class RoutingServiceOptions
{
    public const string SectionName = "RoutingService";

    /// <summary>
    /// Base URL of the NotificationService (e.g., "http://localhost:5200")
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for transient failures
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 500;

    /// <summary>
    /// Circuit breaker failure threshold
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker open duration in seconds
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Whether routing is enabled (if false, events are not published)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException("BaseUrl is required", nameof(BaseUrl));

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("BaseUrl must be a valid absolute URI", nameof(BaseUrl));

        if (TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be positive", nameof(TimeoutSeconds));

        if (RetryCount < 0)
            throw new ArgumentException("RetryCount cannot be negative", nameof(RetryCount));
    }
}
