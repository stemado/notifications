using NotificationService.Client.Models;

namespace NotificationService.Client.Events;

/// <summary>
/// Event published when a file processing error occurs
/// </summary>
public class FileProcessingErrorEvent
{
    /// <summary>
    /// Client identifier
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client display name
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// File path or name that failed
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Type of error (e.g., "FileNotFound", "ParseError", "ValidationError")
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the error
    /// </summary>
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Warning;

    /// <summary>
    /// Related saga ID if part of a workflow
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Whether this error is recoverable
    /// </summary>
    public bool IsRecoverable { get; set; }

    /// <summary>
    /// Suggested resolution steps
    /// </summary>
    public string? Resolution { get; set; }
}
