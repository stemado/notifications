namespace NotificationService.Client.Events;

/// <summary>
/// Event types for service status changes
/// </summary>
public enum ServiceStatusEventType
{
    /// <summary>
    /// Service went down
    /// </summary>
    ServiceDown,

    /// <summary>
    /// Service is degraded (high response time)
    /// </summary>
    ServiceDegraded,

    /// <summary>
    /// Service recovered (back to healthy)
    /// </summary>
    ServiceRecovered,

    /// <summary>
    /// New incident created
    /// </summary>
    IncidentCreated,

    /// <summary>
    /// Incident resolved
    /// </summary>
    IncidentResolved
}

/// <summary>
/// Event published when a service status changes
/// </summary>
public class ServiceStatusEvent
{
    /// <summary>
    /// Unique event ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of status change
    /// </summary>
    public ServiceStatusEventType EventType { get; set; }

    /// <summary>
    /// The service ID (e.g., "census-reconciliation-service")
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// The service display name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Service category (e.g., "BackendApi", "Database", "External")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Previous status before the change
    /// </summary>
    public string PreviousStatus { get; set; } = string.Empty;

    /// <summary>
    /// Current status after the change
    /// </summary>
    public string CurrentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the service is down or degraded
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Response time in milliseconds (for degraded status)
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Threshold that was exceeded (for degraded status)
    /// </summary>
    public int? ThresholdMs { get; set; }

    /// <summary>
    /// Incident ID if this event relates to an incident
    /// </summary>
    public Guid? IncidentId { get; set; }

    /// <summary>
    /// Duration of the incident (for resolved events)
    /// </summary>
    public TimeSpan? IncidentDuration { get; set; }

    /// <summary>
    /// Number of failed health checks during the incident
    /// </summary>
    public int? FailedCheckCount { get; set; }

    /// <summary>
    /// List of dependent services that may be affected
    /// </summary>
    public List<string> AffectedDependents { get; set; } = new();

    /// <summary>
    /// Health check endpoint URL
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
