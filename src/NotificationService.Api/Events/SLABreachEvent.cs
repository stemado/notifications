using NotificationService.Domain.Enums;

namespace NotificationService.Api.Events;

/// <summary>
/// API-side SLA breach event
/// </summary>
public class SLABreachEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string SLAType { get; set; } = string.Empty;
    public int ThresholdMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
}
