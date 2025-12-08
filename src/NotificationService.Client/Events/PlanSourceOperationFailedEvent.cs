using NotificationService.Client.Models;

namespace NotificationService.Client.Events;

/// <summary>
/// Event published when a PlanSource operation fails
/// </summary>
public class PlanSourceOperationFailedEvent
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
    /// Type of operation that failed (e.g., "FileUpload", "Extraction", "FullFilePull")
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Error message describing the failure
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code if available
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether the error is retryable
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Current retry attempt number
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Maximum retries allowed
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Current workflow state
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the failure
    /// </summary>
    public NotificationSeverity Severity { get; set; }

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
}
