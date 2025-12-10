using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Contracts;

/// <summary>
/// Message published when an outbound delivery is created and needs to be sent.
/// MassTransit will use the EF Core outbox to ensure transactional delivery guarantees.
/// </summary>
public record DeliveryRequestedMessage
{
    /// <summary>
    /// The ID of the OutboundDelivery record to process
    /// </summary>
    public required Guid DeliveryId { get; init; }

    /// <summary>
    /// The ID of the parent OutboundEvent
    /// </summary>
    public required Guid OutboundEventId { get; init; }

    /// <summary>
    /// The contact to deliver to
    /// </summary>
    public required Guid ContactId { get; init; }

    /// <summary>
    /// The channel to use for delivery (Email, SMS, Teams)
    /// </summary>
    public required NotificationChannel Channel { get; init; }

    /// <summary>
    /// The recipient role (To, Cc, Bcc)
    /// </summary>
    public required DeliveryRole Role { get; init; }

    /// <summary>
    /// Denormalized recipient address for quick access (email or phone)
    /// </summary>
    public string? RecipientAddress { get; init; }

    /// <summary>
    /// Email subject or notification title
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Message body content
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Client ID for routing/logging purposes
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>
    /// When the delivery was originally created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
