using NotificationService.Client.Models;

namespace NotificationService.Client.Events;

/// <summary>
/// Event published when a workflow escalation is created
/// </summary>
public class EscalationCreatedEvent
{
    /// <summary>
    /// The escalation ID
    /// </summary>
    public Guid EscalationId { get; set; }

    /// <summary>
    /// The related saga/workflow ID
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client display name
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Type of escalation (e.g., "StuckWorkflow", "ImportFailure", "ManualInterventionRequired")
    /// </summary>
    public string EscalationType { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the escalation
    /// </summary>
    public NotificationSeverity Severity { get; set; }

    /// <summary>
    /// Detailed reason for the escalation
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Current workflow state
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// How long the workflow has been in this state
    /// </summary>
    public TimeSpan TimeInState { get; set; }

    /// <summary>
    /// File name being processed (if applicable)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// When the escalation was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Recommended actions to resolve the escalation
    /// </summary>
    public List<string> SuggestedActions { get; set; } = new();
}
