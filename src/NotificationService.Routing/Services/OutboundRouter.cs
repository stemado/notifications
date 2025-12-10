using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Messaging;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service implementation for outbound notification routing.
/// Evaluates routing policies and creates delivery records for matching recipients.
/// </summary>
public class OutboundRouter : IOutboundRouter
{
    private readonly IRoutingPolicyRepository _policyRepository;
    private readonly IOutboundEventRepository _eventRepository;
    private readonly IOutboundDeliveryRepository _deliveryRepository;
    private readonly IDeliveryMessagePublisher _messagePublisher;
    private readonly ILogger<OutboundRouter> _logger;

    public OutboundRouter(
        IRoutingPolicyRepository policyRepository,
        IOutboundEventRepository eventRepository,
        IOutboundDeliveryRepository deliveryRepository,
        IDeliveryMessagePublisher messagePublisher,
        ILogger<OutboundRouter> logger)
    {
        _policyRepository = policyRepository;
        _eventRepository = eventRepository;
        _deliveryRepository = deliveryRepository;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<Guid> PublishAsync(OutboundEvent evt)
    {
        // Create the event
        var createdEvent = await _eventRepository.CreateAsync(evt);
        _logger.LogInformation(
            "Created outbound event {EventId}: {Service}/{Topic} for client {ClientId}",
            createdEvent.Id, evt.Service, evt.Topic, evt.ClientId ?? "(none)");

        // Find matching policies
        var policies = await GetMatchingPoliciesAsync(
            evt.Service, evt.Topic, evt.ClientId, evt.Severity);

        if (policies.Count == 0)
        {
            _logger.LogWarning(
                "No routing policies matched for event {EventId}: {Service}/{Topic} client={ClientId}",
                createdEvent.Id, evt.Service, evt.Topic, evt.ClientId);
            await _eventRepository.MarkProcessedAsync(createdEvent.Id);
            return createdEvent.Id;
        }

        // Create delivery records for each recipient
        var deliveries = new List<OutboundDelivery>();

        foreach (var policy in policies)
        {
            if (policy.RecipientGroup?.Memberships == null)
            {
                continue;
            }

            foreach (var membership in policy.RecipientGroup.Memberships)
            {
                if (membership.Contact == null || !membership.Contact.IsActive)
                {
                    continue;
                }

                // Skip if contact doesn't have the required channel info
                if (policy.Channel == NotificationChannel.Email && string.IsNullOrEmpty(membership.Contact.Email))
                {
                    continue;
                }
                if (policy.Channel == NotificationChannel.SMS && string.IsNullOrEmpty(membership.Contact.Phone))
                {
                    continue;
                }

                deliveries.Add(new OutboundDelivery
                {
                    OutboundEventId = createdEvent.Id,
                    RoutingPolicyId = policy.Id,
                    ContactId = membership.ContactId,
                    Channel = policy.Channel,
                    Role = policy.Role,
                    Status = DeliveryStatus.Pending
                });
            }
        }

        if (deliveries.Count > 0)
        {
            // Create the delivery records in the database
            var createdDeliveries = await _deliveryRepository.CreateManyAsync(deliveries);
            _logger.LogInformation(
                "Created {DeliveryCount} deliveries for event {EventId}",
                createdDeliveries.Count, createdEvent.Id);

            // Publish messages to trigger delivery processing via MassTransit.
            // The EF Core outbox ensures these messages are committed atomically
            // with the delivery records.
            await _messagePublisher.PublishDeliveryRequestsAsync(createdDeliveries, createdEvent);
        }
        else
        {
            _logger.LogWarning(
                "No deliveries created for event {EventId} - no active contacts in matched groups",
                createdEvent.Id);
        }

        // Mark event as processed
        await _eventRepository.MarkProcessedAsync(createdEvent.Id);

        return createdEvent.Id;
    }

    public async Task<List<RoutingPolicy>> GetMatchingPoliciesAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId,
        NotificationSeverity severity)
    {
        return await _policyRepository.GetMatchingPoliciesAsync(service, topic, clientId, severity);
    }

    public async Task<OutboundEvent?> GetEventAsync(Guid id)
    {
        return await _eventRepository.GetByIdAsync(id);
    }

    public async Task<List<OutboundEvent>> GetEventsBySagaAsync(Guid sagaId)
    {
        return await _eventRepository.GetBySagaAsync(sagaId);
    }

    public async Task<List<OutboundEvent>> GetEventsByClientAsync(
        string clientId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return await _eventRepository.GetByClientAsync(clientId, fromDate, toDate);
    }
}
