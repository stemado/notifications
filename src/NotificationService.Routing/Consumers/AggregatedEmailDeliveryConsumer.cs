using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Contracts;
using NotificationService.Routing.Repositories;
using NotificationService.Routing.Services.Channels;

namespace NotificationService.Routing.Consumers;

/// <summary>
/// Consumes batches of DeliveryRequestedMessage and aggregates email deliveries
/// for the same event into a single email with proper TO/CC/BCC recipients.
///
/// Non-email deliveries (SMS, Teams) are processed individually.
/// </summary>
public class AggregatedEmailDeliveryConsumer : IConsumer<Batch<DeliveryRequestedMessage>>
{
    private readonly IOutboundDeliveryRepository _deliveryRepository;
    private readonly IOutboundEventRepository _eventRepository;
    private readonly IAggregatedEmailDispatcher _aggregatedEmailDispatcher;
    private readonly IChannelDispatcher _channelDispatcher;
    private readonly ILogger<AggregatedEmailDeliveryConsumer> _logger;

    public AggregatedEmailDeliveryConsumer(
        IOutboundDeliveryRepository deliveryRepository,
        IOutboundEventRepository eventRepository,
        IAggregatedEmailDispatcher aggregatedEmailDispatcher,
        IChannelDispatcher channelDispatcher,
        ILogger<AggregatedEmailDeliveryConsumer> logger)
    {
        _deliveryRepository = deliveryRepository;
        _eventRepository = eventRepository;
        _aggregatedEmailDispatcher = aggregatedEmailDispatcher;
        _channelDispatcher = channelDispatcher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Batch<DeliveryRequestedMessage>> context)
    {
        var messages = context.Message.ToList();

        _logger.LogInformation(
            "Processing batch of {Count} delivery messages",
            messages.Count);

        // Group messages by OutboundEventId and Channel
        var emailGroups = messages
            .Where(m => m.Message.Channel == NotificationChannel.Email)
            .GroupBy(m => m.Message.OutboundEventId)
            .ToList();

        var nonEmailMessages = messages
            .Where(m => m.Message.Channel != NotificationChannel.Email)
            .ToList();

        // Process email groups (aggregated by event)
        foreach (var group in emailGroups)
        {
            await ProcessEmailGroupAsync(group.Key, group.ToList());
        }

        // Process non-email messages individually
        foreach (var msg in nonEmailMessages)
        {
            await ProcessSingleDeliveryAsync(msg.Message);
        }
    }

    private async Task ProcessEmailGroupAsync(
        Guid eventId,
        List<ConsumeContext<DeliveryRequestedMessage>> messages)
    {
        _logger.LogDebug(
            "Processing email group for event {EventId} with {Count} deliveries",
            eventId, messages.Count);

        // Load all deliveries
        var deliveryIds = messages.Select(m => m.Message.DeliveryId).ToList();
        var deliveries = await _deliveryRepository.GetByIdsAsync(deliveryIds);

        if (deliveries.Count == 0)
        {
            _logger.LogWarning(
                "No deliveries found for event {EventId}, skipping batch",
                eventId);
            return;
        }

        // Filter out already processed deliveries
        var pendingDeliveries = deliveries
            .Where(d => d.Status != DeliveryStatus.Delivered &&
                       d.Status != DeliveryStatus.Cancelled)
            .ToList();

        if (pendingDeliveries.Count == 0)
        {
            _logger.LogInformation(
                "All deliveries for event {EventId} already in terminal state, skipping",
                eventId);
            return;
        }

        // Mark all as processing
        foreach (var delivery in pendingDeliveries)
        {
            await _deliveryRepository.UpdateStatusAsync(delivery.Id, DeliveryStatus.Processing);
        }

        try
        {
            // Load the event
            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null)
            {
                throw new InvalidOperationException(
                    $"OutboundEvent {eventId} not found for deliveries");
            }

            // If only one delivery, process as single (no aggregation needed)
            if (pendingDeliveries.Count == 1)
            {
                await ProcessSingleDeliveryInternalAsync(pendingDeliveries[0], evt);
                return;
            }

            // Dispatch aggregated email
            var result = await _aggregatedEmailDispatcher.DispatchAggregatedAsync(
                pendingDeliveries, evt);

            // Update statuses based on results
            foreach (var delivery in pendingDeliveries)
            {
                if (result.DeliveryResults.TryGetValue(delivery.Id, out var deliveryResult))
                {
                    if (deliveryResult.Success)
                    {
                        await _deliveryRepository.UpdateStatusAsync(
                            delivery.Id,
                            DeliveryStatus.Delivered,
                            externalMessageId: result.ExternalMessageId);
                    }
                    else
                    {
                        await _deliveryRepository.UpdateStatusAsync(
                            delivery.Id,
                            DeliveryStatus.Failed,
                            deliveryResult.ErrorMessage);
                    }
                }
                else
                {
                    // No result for this delivery - mark as failed
                    await _deliveryRepository.UpdateStatusAsync(
                        delivery.Id,
                        DeliveryStatus.Failed,
                        "No result returned from dispatcher");
                }
            }

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully sent aggregated email for event {EventId} to {Count} recipients. MessageId: {MessageId}",
                    eventId, pendingDeliveries.Count, result.ExternalMessageId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send aggregated email for event {EventId}: {Error}",
                    eventId, result.ErrorMessage);

                if (result.IsRetryable)
                {
                    throw new DeliveryFailedException(result.ErrorMessage ?? "Aggregated email delivery failed");
                }
            }
        }
        catch (DeliveryFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing aggregated email for event {EventId}",
                eventId);

            foreach (var delivery in pendingDeliveries)
            {
                await _deliveryRepository.UpdateStatusAsync(
                    delivery.Id,
                    DeliveryStatus.Failed,
                    ex.Message);
            }

            throw;
        }
    }

    private async Task ProcessSingleDeliveryAsync(DeliveryRequestedMessage message)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(message.DeliveryId);
        if (delivery == null)
        {
            _logger.LogWarning("Delivery {DeliveryId} not found, skipping", message.DeliveryId);
            return;
        }

        if (delivery.Status == DeliveryStatus.Delivered ||
            delivery.Status == DeliveryStatus.Cancelled)
        {
            _logger.LogInformation(
                "Delivery {DeliveryId} already in terminal state {Status}, skipping",
                message.DeliveryId, delivery.Status);
            return;
        }

        await _deliveryRepository.UpdateStatusAsync(delivery.Id, DeliveryStatus.Processing);

        try
        {
            var evt = await _eventRepository.GetByIdAsync(message.OutboundEventId);
            if (evt == null)
            {
                throw new InvalidOperationException(
                    $"OutboundEvent {message.OutboundEventId} not found");
            }

            await ProcessSingleDeliveryInternalAsync(delivery, evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing delivery {DeliveryId}", message.DeliveryId);
            await _deliveryRepository.UpdateStatusAsync(
                delivery.Id,
                DeliveryStatus.Failed,
                ex.Message);
            throw;
        }
    }

    private async Task ProcessSingleDeliveryInternalAsync(
        Domain.Models.OutboundDelivery delivery,
        Domain.Models.OutboundEvent evt)
    {
        var result = await _channelDispatcher.DispatchAsync(delivery, evt);

        if (result.Success)
        {
            await _deliveryRepository.UpdateStatusAsync(delivery.Id, DeliveryStatus.Delivered);
            _logger.LogInformation(
                "Successfully delivered {DeliveryId} via {Channel}",
                delivery.Id, delivery.Channel);
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

            if (result.IsRetryable)
            {
                throw new DeliveryFailedException(result.ErrorMessage ?? "Delivery failed");
            }
        }
    }
}
