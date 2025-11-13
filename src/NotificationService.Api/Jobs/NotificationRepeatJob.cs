using Hangfire;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.Jobs;

/// <summary>
/// Background job that repeats notifications based on RepeatInterval
/// Runs every 5 minutes to check for notifications due for repeat
/// </summary>
public class NotificationRepeatJob
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationRepeatJob> _logger;

    public NotificationRepeatJob(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationRepeatJob> logger)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task Execute()
    {
        try
        {
            _logger.LogInformation("Starting notification repeat job");

            // Find notifications that need to repeat
            var notifications = await _notificationService.GetNotificationsDueForRepeatAsync();

            _logger.LogInformation("Found {Count} notifications due for repeat", notifications.Count);

            foreach (var notification in notifications)
            {
                try
                {
                    // Update last repeated time
                    await _notificationService.UpdateLastRepeatedAsync(notification.Id);

                    // Push via SignalR
                    await _hubContext.Clients
                        .User(notification.UserId.ToString())
                        .SendAsync("RepeatNotification", notification);

                    // Also send to ops-team if TenantId is null
                    if (notification.TenantId == null)
                    {
                        await _hubContext.Clients
                            .Group("ops-team")
                            .SendAsync("RepeatNotification", notification);
                    }

                    _logger.LogInformation(
                        "Repeated notification {NotificationId} to user {UserId}",
                        notification.Id, notification.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error repeating notification {NotificationId}",
                        notification.Id);
                    // Continue with other notifications
                }
            }

            _logger.LogInformation("Notification repeat job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification repeat job");
            throw;
        }
    }
}
