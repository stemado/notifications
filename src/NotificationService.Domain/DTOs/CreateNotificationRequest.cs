using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;

namespace NotificationService.Domain.DTOs;

/// <summary>
/// Request model for creating a new notification
/// </summary>
public class CreateNotificationRequest
{
    // Ownership
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }

    // Content
    public NotificationSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Source
    public Guid? SagaId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? EventId { get; set; }
    public string? EventType { get; set; }

    // Behavior
    public int? RepeatInterval { get; set; }
    public bool RequiresAck { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Grouping
    public string? GroupKey { get; set; }

    // Actions & Metadata
    public List<NotificationAction> Actions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
