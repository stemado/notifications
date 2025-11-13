using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Models;

/// <summary>
/// Tracks delivery attempts for a notification across different channels (Phase 2)
/// </summary>
public class NotificationDelivery
{
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the notification being delivered
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// Delivery channel used
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// When the notification was successfully delivered
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// When the delivery failed
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of delivery attempts made
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Navigation property to parent notification
    /// </summary>
    public Notification? Notification { get; set; }
}
