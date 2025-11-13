using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Email notification channel (Phase 2 - STUB)
/// </summary>
public class EmailChannel : INotificationChannel
{
    public string ChannelName => "Email";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        // Phase 2: Implement email sending
        // - Load email template
        // - Get user email address
        // - Send via SMTP or email service
        // - Track delivery status
        throw new NotImplementedException("Phase 2: Implement email sending");
    }
}
