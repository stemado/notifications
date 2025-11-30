namespace NotificationService.Client.Events;

/// <summary>
/// Event published when an import workflow completes successfully
/// </summary>
public class ImportCompletedEvent
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
    /// Name of the file that was processed
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Total records in the file
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Successfully processed records
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Failed records
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Skipped records
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// New hires detected
    /// </summary>
    public int NewHireCount { get; set; }

    /// <summary>
    /// Terminations detected
    /// </summary>
    public int TerminationCount { get; set; }

    /// <summary>
    /// Demographic changes detected
    /// </summary>
    public int DemographicChangeCount { get; set; }

    /// <summary>
    /// When the import started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the import completed
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total processing duration
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
