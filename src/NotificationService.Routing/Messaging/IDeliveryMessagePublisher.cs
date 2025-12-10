using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Messaging;

/// <summary>
/// Interface for publishing delivery messages to the message bus.
/// This abstraction allows the repository to publish messages without
/// directly depending on MassTransit.
/// </summary>
public interface IDeliveryMessagePublisher
{
    /// <summary>
    /// Publishes delivery request messages for all provided deliveries.
    /// Called after deliveries are persisted to the database.
    /// MassTransit's outbox ensures both the DB write and message publish
    /// are committed atomically.
    /// </summary>
    Task PublishDeliveryRequestsAsync(
        List<OutboundDelivery> deliveries,
        OutboundEvent evt,
        CancellationToken cancellationToken = default);
}
