using System.Text.Json;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// An event published by a service that needs to be routed to recipients.
/// This is the inbound event that triggers routing policy evaluation.
/// </summary>
public class OutboundEvent
{
    public Guid Id { get; set; }

    /// <summary>
    /// The source service publishing this event
    /// </summary>
    public SourceService Service { get; set; }

    /// <summary>
    /// The notification topic
    /// </summary>
    public NotificationTopic Topic { get; set; }

    /// <summary>
    /// The client this event is associated with
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The severity of this notification
    /// </summary>
    public NotificationSeverity Severity { get; set; }

    // Template/content
    /// <summary>
    /// Optional template ID to use for rendering
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Subject line for the notification
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Body content of the notification
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Additional payload data for template rendering.
    /// Using JsonElement for proper JSON round-tripping.
    /// </summary>
    public Dictionary<string, JsonElement> Payload { get; set; } = new();

    // Tracking
    /// <summary>
    /// Associated saga/workflow ID if applicable
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public Guid? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the event was fully processed (all deliveries created)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public List<OutboundDelivery> Deliveries { get; set; } = new();
}
