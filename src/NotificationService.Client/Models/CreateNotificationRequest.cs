namespace NotificationService.Client.Models;

/// <summary>
/// Request model for creating a notification via the NotificationService API
/// </summary>
public class CreateNotificationRequest
{
    /// <summary>
    /// User ID to send notification to. Required for user-specific notifications.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Notification severity level
    /// </summary>
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    /// <summary>
    /// Short title/subject of the notification
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full notification message body
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Related saga/workflow ID (for census reconciliation)
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Related client ID
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Related event ID for correlation
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Event type identifier (e.g., "SagaStuck", "ImportCompleted")
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Repeat interval in minutes for recurring notifications (null = no repeat)
    /// </summary>
    public int? RepeatInterval { get; set; }

    /// <summary>
    /// Whether this notification requires explicit acknowledgment
    /// </summary>
    public bool RequiresAck { get; set; }

    /// <summary>
    /// When this notification expires and should be auto-dismissed
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Group key for deduplication - notifications with same group key will be updated rather than duplicated
    /// </summary>
    public string? GroupKey { get; set; }

    /// <summary>
    /// Actionable buttons/links for the notification
    /// </summary>
    public List<NotificationAction> Actions { get; set; } = new();

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
