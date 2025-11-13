using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Interface for notification delivery channels (Phase 2)
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Name of the channel (e.g., "SignalR", "Email", "SMS", "Slack")
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// Delivers a notification to a user via this channel
    /// </summary>
    Task DeliverAsync(Notification notification, Guid userId);
}
