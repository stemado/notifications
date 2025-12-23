using NotificationService.Client.Routing.Models;

namespace NotificationService.Client.Routing;

/// <summary>
/// Client for interacting with the NotificationService routing API.
/// Publishes outbound events that are routed to recipients based on policies.
/// </summary>
public interface IRoutingServiceClient
{
    /// <summary>
    /// Publishes an outbound event for routing.
    /// The routing service evaluates policies and creates deliveries for matching recipients.
    /// </summary>
    /// <param name="request">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with event ID and delivery count</returns>
    /// <exception cref="RoutingServiceException">Thrown when the request fails</exception>
    Task<OutboundEventResponse> PublishEventAsync(
        OutboundEventRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the routing configuration for a specific client.
    /// Includes policies, contacts, and stats.
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Client routing configuration</returns>
    Task<ClientRoutingConfiguration> GetClientRoutingAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets policies that would match a specific service/topic/client combination.
    /// Useful for previewing who would receive a notification.
    /// </summary>
    /// <param name="service">Source service</param>
    /// <param name="topic">Notification topic</param>
    /// <param name="clientId">Client ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching policies</returns>
    Task<List<RoutingPolicySummary>> GetMatchingPoliciesAsync(
        string service,
        string topic,
        string? clientId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the routing service is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
