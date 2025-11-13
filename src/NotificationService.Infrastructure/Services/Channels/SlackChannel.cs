using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Slack notification channel (Phase 3 - STUB)
/// </summary>
public class SlackChannel : INotificationChannel
{
    public string ChannelName => "Slack";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        // Phase 3: Implement Slack integration
        // - Get user Slack webhook or bot token
        // - Format message for Slack
        // - Send via Slack API
        // - Track delivery status
        throw new NotImplementedException("Phase 3: Implement Slack integration");
    }
}
