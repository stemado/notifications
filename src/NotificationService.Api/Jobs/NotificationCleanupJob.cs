using Quartz;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.Jobs;

/// <summary>
/// Background job that cleans up old notifications
/// Runs daily at 2 AM to expire old notifications and delete acknowledged ones
/// </summary>
public class NotificationCleanupJob : IJob
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationCleanupJob> _logger;

    public NotificationCleanupJob(
        INotificationService notificationService,
        ILogger<NotificationCleanupJob> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting notification cleanup job");

            // Expire old notifications (based on ExpiresAt field)
            await _notificationService.ExpireOldNotificationsAsync();
            _logger.LogInformation("Expired old notifications");

            // Delete acknowledged notifications > 30 days old
            await _notificationService.DeleteAcknowledgedAsync(daysOld: 30);
            _logger.LogInformation("Deleted acknowledged notifications older than 30 days");

            _logger.LogInformation("Notification cleanup job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification cleanup job");
            throw;
        }
    }
}
