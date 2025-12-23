namespace NotificationService.Client.Routing.Models;

/// <summary>
/// Response from publishing an outbound event
/// </summary>
public record OutboundEventResponse
{
    /// <summary>
    /// The created event ID
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Number of deliveries created based on matching policies
    /// </summary>
    public int DeliveryCount { get; init; }

    /// <summary>
    /// Whether any policies matched
    /// </summary>
    public bool HasMatchingPolicies { get; init; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; init; }
}
