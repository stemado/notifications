namespace NotificationService.Client.Events;

/// <summary>
/// Event published when template files have been queued for import processing.
/// This triggers the Import History Scheduler to schedule a delayed check.
/// </summary>
public class TemplatesQueuedEvent
{
    /// <summary>
    /// The saga/workflow ID
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Client identifier (e.g., "4811266")
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client display name
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Number of template files queued
    /// </summary>
    public int TemplateCount { get; set; }

    /// <summary>
    /// Queue IDs assigned to the templates
    /// </summary>
    public List<int> QueueIds { get; set; } = new();

    /// <summary>
    /// Template file names that were queued
    /// </summary>
    public List<string> TemplateFiles { get; set; } = new();

    /// <summary>
    /// Import types included (e.g., NewHire, Termination, Demographic)
    /// </summary>
    public List<string> ImportTypes { get; set; } = new();

    /// <summary>
    /// Delay in minutes before checking import history (default: 90)
    /// </summary>
    public int DelayMinutes { get; set; } = 90;

    /// <summary>
    /// When the templates were queued
    /// </summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
