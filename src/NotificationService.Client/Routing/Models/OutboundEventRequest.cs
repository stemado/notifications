namespace NotificationService.Client.Routing.Models;

/// <summary>
/// Request to publish an outbound event for routing to recipients.
/// </summary>
public record OutboundEventRequest
{
    /// <summary>
    /// The source service publishing this event (e.g., "CensusOrchestration", "ImportProcessor")
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// The notification topic (e.g., "DailyImportSuccess", "OrchestrationServiceError")
    /// </summary>
    public required string Topic { get; init; }

    /// <summary>
    /// The client ID this event is associated with
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Severity level: Info, Warning, Error, Critical
    /// </summary>
    public string Severity { get; init; } = "Info";

    /// <summary>
    /// Optional template ID for rendering
    /// </summary>
    public string? TemplateId { get; init; }

    /// <summary>
    /// Subject line for the notification
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Body content (can be HTML for email)
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Associated saga ID for correlation
    /// </summary>
    public Guid? SagaId { get; init; }

    /// <summary>
    /// Additional payload data for template rendering
    /// </summary>
    public Dictionary<string, object>? Payload { get; init; }
}
