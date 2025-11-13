using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// SignalR notification channel (Phase 2 - STUB)
/// In Phase 1, SignalR push is handled directly in event handlers
/// </summary>
public class SignalRChannel : INotificationChannel
{
    public string ChannelName => "SignalR";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        // Phase 2: Move SignalR push logic here from event handlers
        // For now, SignalR delivery is handled in event handlers directly
        throw new NotImplementedException("Phase 2: Move SignalR push logic here from event handlers");
    }
}
