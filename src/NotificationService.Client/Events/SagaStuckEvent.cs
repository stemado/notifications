namespace NotificationService.Client.Events;

/// <summary>
/// Event published when a saga/workflow has been stuck for too long
/// </summary>
public class SagaStuckEvent
{
    /// <summary>
    /// The saga/workflow ID
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Client identifier (e.g., "HenryCounty")
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client display name
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// How long the saga has been stuck
    /// </summary>
    public TimeSpan StuckDuration { get; set; }

    /// <summary>
    /// Current workflow state
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// File name being processed (if applicable)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// When the stuck condition was detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
