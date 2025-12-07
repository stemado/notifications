using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service interface for outbound notification routing
/// </summary>
public interface IOutboundRouter
{
    /// <summary>
    /// Publish an event to be routed to appropriate recipients.
    /// This evaluates routing policies and creates delivery records.
    /// </summary>
    Task<Guid> PublishAsync(OutboundEvent evt);

    /// <summary>
    /// Get routing policies that match the given criteria.
    /// </summary>
    Task<List<RoutingPolicy>> GetMatchingPoliciesAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId,
        NotificationSeverity severity);

    /// <summary>
    /// Get an event by ID with its deliveries.
    /// </summary>
    Task<OutboundEvent?> GetEventAsync(Guid id);

    /// <summary>
    /// Get events for a specific saga/workflow.
    /// </summary>
    Task<List<OutboundEvent>> GetEventsBySagaAsync(Guid sagaId);

    /// <summary>
    /// Get events for a specific client.
    /// </summary>
    Task<List<OutboundEvent>> GetEventsByClientAsync(string clientId, DateTime? fromDate = null, DateTime? toDate = null);
}
