using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services.Channels;

/// <summary>
/// Dispatches multiple outbound deliveries as a single aggregated email with TO/CC/BCC.
/// </summary>
public interface IAggregatedEmailDispatcher
{
    /// <summary>
    /// Dispatches multiple deliveries for the same event as a single email.
    /// Deliveries are grouped by their Role property (To, Cc, Bcc).
    /// </summary>
    /// <param name="deliveries">All deliveries for the event (must all be Email channel)</param>
    /// <param name="evt">The outbound event containing subject and body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing per-delivery status information</returns>
    Task<AggregatedDispatchResult> DispatchAggregatedAsync(
        List<OutboundDelivery> deliveries,
        OutboundEvent evt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an aggregated email dispatch operation.
/// Contains the overall result plus per-delivery status for tracking.
/// </summary>
public record AggregatedDispatchResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsRetryable { get; init; } = true;
    public string? ExternalMessageId { get; init; }

    /// <summary>
    /// Status for each delivery ID in the batch.
    /// All deliveries share the same message ID on success.
    /// </summary>
    public Dictionary<Guid, DeliveryResult> DeliveryResults { get; init; } = new();

    public static AggregatedDispatchResult Succeeded(
        string? externalMessageId,
        Dictionary<Guid, DeliveryResult> deliveryResults) =>
        new()
        {
            Success = true,
            ExternalMessageId = externalMessageId,
            DeliveryResults = deliveryResults
        };

    public static AggregatedDispatchResult Failed(
        string errorMessage,
        bool isRetryable = true,
        Dictionary<Guid, DeliveryResult>? deliveryResults = null) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            IsRetryable = isRetryable,
            DeliveryResults = deliveryResults ?? new()
        };
}

/// <summary>
/// Result for an individual delivery within an aggregated batch.
/// </summary>
public record DeliveryResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Email { get; init; }

    public static DeliveryResult Succeeded(string email) =>
        new() { Success = true, Email = email };

    public static DeliveryResult Failed(string email, string errorMessage) =>
        new() { Success = false, Email = email, ErrorMessage = errorMessage };
}
