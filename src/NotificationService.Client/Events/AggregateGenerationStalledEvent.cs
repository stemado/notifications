using NotificationService.Client.Models;

namespace NotificationService.Client.Events;

/// <summary>
/// Event published when aggregate generation appears stalled
/// </summary>
public class AggregateGenerationStalledEvent
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
    /// Number of times we've checked for the aggregate
    /// </summary>
    public int WaitCount { get; set; }

    /// <summary>
    /// Maximum wait count before escalation
    /// </summary>
    public int MaxWaitCount { get; set; }

    /// <summary>
    /// Total minutes spent waiting
    /// </summary>
    public int MinutesWaiting { get; set; }

    /// <summary>
    /// File being processed (if applicable)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Severity of the stall
    /// </summary>
    public NotificationSeverity Severity { get; set; }

    /// <summary>
    /// When the stall was detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
