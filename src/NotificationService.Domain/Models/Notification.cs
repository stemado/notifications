using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Models;

/// <summary>
/// Core notification entity with full architecture support
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    // Ownership
    /// <summary>
    /// User who should see this notification
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant scenarios (NULL = ops team, NotNull = client portal)
    /// </summary>
    public Guid? TenantId { get; set; }

    // Content
    /// <summary>
    /// Severity level of the notification
    /// </summary>
    public NotificationSeverity Severity { get; set; }

    /// <summary>
    /// Notification title (max 200 characters)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed notification message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    // Source
    /// <summary>
    /// Link to associated saga
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Link to associated client
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Link to domain event (event sourcing ready)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Type of event that triggered this notification
    /// </summary>
    public string? EventType { get; set; }

    // Lifecycle
    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the notification was acknowledged by the user
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// When the notification was dismissed
    /// </summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>
    /// When the notification should expire
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Behavior
    /// <summary>
    /// Minutes between notification repeats (for persistent issues)
    /// </summary>
    public int? RepeatInterval { get; set; }

    /// <summary>
    /// Last time this notification was repeated
    /// </summary>
    public DateTime? LastRepeatedAt { get; set; }

    /// <summary>
    /// Whether this notification requires acknowledgment
    /// </summary>
    public bool RequiresAck { get; set; }

    // Grouping
    /// <summary>
    /// Key for grouping/deduplicating similar notifications (e.g., "saga:stuck:{sagaId}")
    /// </summary>
    public string? GroupKey { get; set; }

    /// <summary>
    /// Number of times this notification has been triggered (for grouped notifications)
    /// </summary>
    public int GroupCount { get; set; } = 1;

    // Actions & Metadata
    /// <summary>
    /// Available actions for this notification
    /// </summary>
    public List<NotificationAction> Actions { get; set; } = new();

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
