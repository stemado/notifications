using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Tracks delivery of a routed notification to a specific recipient.
/// One OutboundEvent can result in many OutboundDeliveries (one per recipient per channel).
/// </summary>
public class OutboundDelivery
{
    public Guid Id { get; set; }

    public Guid OutboundEventId { get; set; }
    public Guid RoutingPolicyId { get; set; }
    public Guid ContactId { get; set; }

    /// <summary>
    /// The channel used for this delivery
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// The recipient role (To, CC, BCC)
    /// </summary>
    public DeliveryRole Role { get; set; }

    /// <summary>
    /// Current delivery status
    /// </summary>
    public DeliveryStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of delivery attempts
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// When to retry next (if failed and retryable)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    // Navigation
    public OutboundEvent? OutboundEvent { get; set; }
    public RoutingPolicy? RoutingPolicy { get; set; }
    public Contact? Contact { get; set; }
}
