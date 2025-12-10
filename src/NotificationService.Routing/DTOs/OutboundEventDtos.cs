using System.Text.Json;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// Request to publish an outbound event
/// </summary>
public record PublishEventRequest
{
    public required SourceService Service { get; init; }
    public required NotificationTopic Topic { get; init; }
    public string? ClientId { get; init; }
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Info;
    public string? TemplateId { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public Dictionary<string, JsonElement>? Payload { get; init; }
    public Guid? SagaId { get; init; }
    public Guid? CorrelationId { get; init; }
}

/// <summary>
/// Summary view of an outbound event
/// </summary>
public record OutboundEventSummary
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string Topic { get; init; }
    public string? ClientId { get; init; }
    public required string Severity { get; init; }
    public string? Subject { get; init; }
    public Guid? SagaId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int DeliveryCount { get; init; }
    public int PendingCount { get; init; }
    public int DeliveredCount { get; init; }
    public int FailedCount { get; init; }
}

/// <summary>
/// Detailed view of an outbound event with deliveries
/// </summary>
public record OutboundEventDetails
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string Topic { get; init; }
    public string? ClientId { get; init; }
    public required string Severity { get; init; }
    public string? TemplateId { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public Dictionary<string, JsonElement> Payload { get; init; } = new();
    public Guid? SagaId { get; init; }
    public Guid? CorrelationId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public List<DeliveryInfo> Deliveries { get; init; } = new();
}

/// <summary>
/// Delivery info for an event
/// </summary>
public record DeliveryInfo
{
    public Guid Id { get; init; }
    public Guid ContactId { get; init; }
    public required string ContactName { get; init; }
    public required string ContactEmail { get; init; }
    public required string Channel { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int AttemptCount { get; init; }
}
