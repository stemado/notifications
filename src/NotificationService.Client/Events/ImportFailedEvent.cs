namespace NotificationService.Client.Events;

/// <summary>
/// Event published when an import workflow fails
/// </summary>
public class ImportFailedEvent
{
    /// <summary>
    /// The saga/workflow ID
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
    /// Name of the file that failed
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Error message describing the failure
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Exception type if applicable
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Stack trace for debugging (may be truncated)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Workflow state when failure occurred
    /// </summary>
    public string FailedAtState { get; set; } = string.Empty;

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// When the failure occurred
    /// </summary>
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Whether this failure was escalated
    /// </summary>
    public bool WasEscalated { get; set; }
}
