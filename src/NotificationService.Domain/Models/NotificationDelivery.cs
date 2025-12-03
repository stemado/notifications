using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Models;

/// <summary>
/// Tracks delivery attempts for a notification across different channels
/// Supports both queued (pending) deliveries and completed delivery records
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
    /// Current status of this delivery
    /// </summary>
    public DeliveryStatus Status { get; set; }

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
    /// Maximum number of retry attempts allowed
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// When the next retry should occur (for failed deliveries)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// When this delivery record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Raw response data from delivery service (JSON)
    /// </summary>
    public string? ResponseData { get; set; }

    /// <summary>
    /// Navigation property to parent notification
    /// </summary>
    public Notification? Notification { get; set; }
}
