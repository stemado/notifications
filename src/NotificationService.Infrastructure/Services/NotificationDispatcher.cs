using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services.Channels;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Multi-channel notification dispatcher (Phase 2)
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly IUserPreferenceService _preferences;
    private readonly ISubscriptionService _subscriptions;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        IUserPreferenceService preferences,
        ISubscriptionService subscriptions,
        ILogger<NotificationDispatcher> logger)
    {
        _channels = channels;
        _preferences = preferences;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public async Task DispatchAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation(
                "Dispatching notification {NotificationId} to user {UserId} via configured channels",
                notification.Id, notification.UserId);

            // Check if user should receive this notification based on subscriptions
            var shouldReceive = await _subscriptions.ShouldReceiveNotificationAsync(notification.UserId, notification);
            if (!shouldReceive)
            {
                _logger.LogInformation(
                    "User {UserId} has no subscriptions matching notification {NotificationId}, skipping dispatch",
                    notification.UserId, notification.Id);
                return;
            }

            // Dispatch to each enabled channel
            var tasks = new List<Task>();

            foreach (var channel in _channels)
            {
                try
                {
                    // Parse channel enum from channel name
                    if (!Enum.TryParse<NotificationChannel>(channel.ChannelName, out var channelEnum))
                    {
                        _logger.LogWarning("Unknown channel: {ChannelName}", channel.ChannelName);
                        continue;
                    }

                    // Check if channel is enabled for this user and severity
                    var isEnabled = await _preferences.IsChannelEnabledAsync(
                        notification.UserId,
                        channelEnum,
                        notification.Severity);

                    if (isEnabled)
                    {
                        _logger.LogDebug(
                            "Delivering notification {NotificationId} via {Channel}",
                            notification.Id, channel.ChannelName);

                        // Deliver asynchronously
                        tasks.Add(channel.DeliverAsync(notification, notification.UserId));
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Channel {Channel} is disabled for user {UserId} or severity {Severity} is below threshold",
                            channel.ChannelName, notification.UserId, notification.Severity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error checking channel {Channel} for notification {NotificationId}",
                        channel.ChannelName, notification.Id);
                }
            }

            // Wait for all deliveries to complete
            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Notification {NotificationId} dispatched to {Count} channels",
                notification.Id, tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error dispatching notification {NotificationId}",
                notification.Id);
            throw;
        }
    }
}
