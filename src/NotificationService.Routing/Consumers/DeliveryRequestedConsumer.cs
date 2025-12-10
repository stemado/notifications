using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Contracts;
using NotificationService.Routing.Repositories;
using NotificationService.Routing.Services.Channels;

namespace NotificationService.Routing.Consumers;

/// <summary>
/// Consumes DeliveryRequestedMessage and dispatches to the appropriate channel.
/// This consumer is responsible for:
/// 1. Loading the OutboundDelivery from the database
/// 2. Dispatching to the correct channel (Email, SMS, Teams)
/// 3. Updating the delivery status based on the result
/// </summary>
public class DeliveryRequestedConsumer : IConsumer<DeliveryRequestedMessage>
{
    private readonly IOutboundDeliveryRepository _deliveryRepository;
    private readonly IOutboundEventRepository _eventRepository;
    private readonly IChannelDispatcher _channelDispatcher;
    private readonly ILogger<DeliveryRequestedConsumer> _logger;

    public DeliveryRequestedConsumer(
        IOutboundDeliveryRepository deliveryRepository,
        IOutboundEventRepository eventRepository,
        IChannelDispatcher channelDispatcher,
        ILogger<DeliveryRequestedConsumer> logger)
    {
        _deliveryRepository = deliveryRepository;
        _eventRepository = eventRepository;
        _channelDispatcher = channelDispatcher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeliveryRequestedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing delivery {DeliveryId} for event {EventId} via {Channel}",
            message.DeliveryId, message.OutboundEventId, message.Channel);

        // Load the delivery with its related entities
        var delivery = await _deliveryRepository.GetByIdAsync(message.DeliveryId);
        if (delivery == null)
        {
            _logger.LogWarning(
                "Delivery {DeliveryId} not found, skipping",
                message.DeliveryId);
            return;
        }

        // Check if already processed (idempotency)
        if (delivery.Status == DeliveryStatus.Delivered ||
            delivery.Status == DeliveryStatus.Cancelled)
        {
            _logger.LogInformation(
                "Delivery {DeliveryId} already in terminal state {Status}, skipping",
                message.DeliveryId, delivery.Status);
            return;
        }

        // Mark as processing
        await _deliveryRepository.UpdateStatusAsync(delivery.Id, DeliveryStatus.Processing);

        try
        {
            // Load the event for content
            var evt = await _eventRepository.GetByIdAsync(message.OutboundEventId);
            if (evt == null)
            {
                throw new InvalidOperationException(
                    $"OutboundEvent {message.OutboundEventId} not found for delivery {message.DeliveryId}");
            }

            // Dispatch to the appropriate channel
            var result = await _channelDispatcher.DispatchAsync(delivery, evt);

            if (result.Success)
            {
                await _deliveryRepository.UpdateStatusAsync(delivery.Id, DeliveryStatus.Delivered);
                _logger.LogInformation(
                    "Successfully delivered {DeliveryId} via {Channel} to {Contact}",
                    delivery.Id, delivery.Channel, delivery.Contact?.Email ?? delivery.Contact?.Phone ?? "unknown");
            }
            else
            {
                await _deliveryRepository.UpdateStatusAsync(
                    delivery.Id,
                    DeliveryStatus.Failed,
                    result.ErrorMessage);

                _logger.LogWarning(
                    "Failed to deliver {DeliveryId} via {Channel}: {Error}",
                    delivery.Id, delivery.Channel, result.ErrorMessage);

                // MassTransit will handle retry based on configuration
                if (result.IsRetryable)
                {
                    throw new DeliveryFailedException(result.ErrorMessage ?? "Delivery failed");
                }
            }
        }
        catch (DeliveryFailedException)
        {
            // Re-throw to let MassTransit handle retry
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing delivery {DeliveryId}",
                message.DeliveryId);

            await _deliveryRepository.UpdateStatusAsync(
                delivery.Id,
                DeliveryStatus.Failed,
                ex.Message);

            throw;
        }
    }
}

/// <summary>
/// Exception thrown when delivery fails and should be retried
/// </summary>
public class DeliveryFailedException : Exception
{
    public DeliveryFailedException(string message) : base(message) { }
    public DeliveryFailedException(string message, Exception inner) : base(message, inner) { }
}
