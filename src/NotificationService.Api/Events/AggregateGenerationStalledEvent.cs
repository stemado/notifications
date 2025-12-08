using NotificationService.Domain.Enums;

namespace NotificationService.Api.Events;

/// <summary>
/// API-side aggregate generation stalled event
/// </summary>
public class AggregateGenerationStalledEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int WaitCount { get; set; }
    public int MaxWaitCount { get; set; }
    public int MinutesWaiting { get; set; }
    public string? FileName { get; set; }
    public NotificationSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
}
