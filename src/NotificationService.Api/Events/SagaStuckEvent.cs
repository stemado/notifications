namespace NotificationService.Api.Events;

/// <summary>
/// Domain event raised when a saga is stuck
/// </summary>
public class SagaStuckEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public TimeSpan StuckDuration { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;
}
