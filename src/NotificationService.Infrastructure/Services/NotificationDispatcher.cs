using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services.Channels;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Multi-channel notification dispatcher (Phase 2 - STUB)
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    // Phase 2: Uncomment when user preference service is implemented
    // private readonly IUserPreferenceService _preferences;
    // private readonly IUserService _userService;

    public NotificationDispatcher(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels;
    }

    public async Task DispatchAsync(Notification notification)
    {
        // Phase 1: Only SignalR (already implemented in event handlers)
        // Phase 2: Uncomment below

        // var user = await _userService.GetByIdAsync(notification.UserId);
        // var prefs = await _preferences.GetForUserAsync(notification.UserId);
        //
        // foreach (var channel in _channels)
        // {
        //     if (prefs.IsEnabled(channel.ChannelName, notification.Severity))
        //     {
        //         await channel.DeliverAsync(notification, user);
        //         await RecordDeliveryAsync(notification.Id, channel.ChannelName);
        //     }
        // }

        await Task.CompletedTask;
    }

    // Phase 2: Implement delivery tracking
    // private async Task RecordDeliveryAsync(Guid notificationId, string channelName)
    // {
    //     var delivery = new NotificationDelivery
    //     {
    //         NotificationId = notificationId,
    //         Channel = Enum.Parse<NotificationChannel>(channelName),
    //         DeliveredAt = DateTime.UtcNow,
    //         AttemptCount = 1
    //     };
    //     await _deliveryRepository.CreateAsync(delivery);
    // }
}
