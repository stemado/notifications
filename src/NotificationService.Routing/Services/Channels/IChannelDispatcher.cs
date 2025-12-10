using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services.Channels;

/// <summary>
/// Dispatches outbound deliveries to the appropriate channel (Email, SMS, Teams).
/// </summary>
public interface IChannelDispatcher
{
    /// <summary>
    /// Dispatches a delivery to the appropriate channel based on the delivery's Channel property.
    /// </summary>
    Task<ChannelDispatchResult> DispatchAsync(
        OutboundDelivery delivery,
        OutboundEvent evt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a channel dispatch operation
/// </summary>
public record ChannelDispatchResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsRetryable { get; init; } = true;
    public string? ExternalMessageId { get; init; }

    public static ChannelDispatchResult Succeeded(string? externalMessageId = null) =>
        new() { Success = true, ExternalMessageId = externalMessageId };

    public static ChannelDispatchResult Failed(string errorMessage, bool isRetryable = true) =>
        new() { Success = false, ErrorMessage = errorMessage, IsRetryable = isRetryable };
}
