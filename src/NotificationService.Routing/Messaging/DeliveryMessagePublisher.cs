using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Routing.Contracts;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Messaging;

/// <summary>
/// MassTransit-based implementation of delivery message publishing.
/// Uses IPublishEndpoint to publish messages through the configured bus.
/// When using the EF Core outbox, messages are staged in the outbox table
/// and committed with the same transaction as the delivery records.
/// </summary>
public class DeliveryMessagePublisher : IDeliveryMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeliveryMessagePublisher> _logger;

    public DeliveryMessagePublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<DeliveryMessagePublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishDeliveryRequestsAsync(
        List<OutboundDelivery> deliveries,
        OutboundEvent evt,
        CancellationToken cancellationToken = default)
    {
        foreach (var delivery in deliveries)
        {
            var message = new DeliveryRequestedMessage
            {
                DeliveryId = delivery.Id,
                OutboundEventId = delivery.OutboundEventId,
                ContactId = delivery.ContactId,
                Channel = delivery.Channel,
                Role = delivery.Role,
                RecipientAddress = GetRecipientAddress(delivery),
                Subject = evt.Subject,
                Body = evt.Body,
                ClientId = evt.ClientId,
                CorrelationId = evt.CorrelationId,
                CreatedAt = delivery.CreatedAt
            };

            await _publishEndpoint.Publish(message, cancellationToken);

            _logger.LogDebug(
                "Published DeliveryRequestedMessage for delivery {DeliveryId} via {Channel} to {ContactId}",
                delivery.Id, delivery.Channel, delivery.ContactId);
        }

        _logger.LogInformation(
            "Published {Count} delivery request messages for event {EventId}",
            deliveries.Count, evt.Id);
    }

    private static string? GetRecipientAddress(OutboundDelivery delivery)
    {
        // The Contact navigation property may not be loaded at this point.
        // The consumer will load the full contact details.
        return delivery.Contact?.Email ?? delivery.Contact?.Phone;
    }
}
