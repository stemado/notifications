using NotificationService.Client.Models;

namespace NotificationService.Client.Events;

/// <summary>
/// Event published when an SLA breach is detected for a workflow
/// </summary>
public class SLABreachEvent
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
    /// Type of SLA breached (e.g., "ProcessingTime", "CompletionTarget")
    /// </summary>
    public string SLAType { get; set; } = string.Empty;

    /// <summary>
    /// The threshold in minutes that was exceeded
    /// </summary>
    public int ThresholdMinutes { get; set; }

    /// <summary>
    /// The actual time in minutes
    /// </summary>
    public int ActualMinutes { get; set; }

    /// <summary>
    /// Current workflow state when breach was detected
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the breach
    /// </summary>
    public NotificationSeverity Severity { get; set; }

    /// <summary>
    /// When the breach was detected
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
