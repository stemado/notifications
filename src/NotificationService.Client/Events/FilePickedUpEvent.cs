namespace NotificationService.Client.Events;

/// <summary>
/// Event published when a census file is picked up and registered for processing
/// </summary>
public class FilePickedUpEvent
{
    /// <summary>
    /// The saga/workflow ID created for this file
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
    /// Name of the file that was picked up
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// When the file was picked up
    /// </summary>
    public DateTime PickedUpAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
