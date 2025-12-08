using NotificationService.Domain.Enums;

namespace NotificationService.Api.Events;

/// <summary>
/// API-side PlanSource operation failed event
/// </summary>
public class PlanSourceOperationFailedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public bool IsRetryable { get; set; }
    public int AttemptNumber { get; set; }
    public int MaxRetries { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
}
