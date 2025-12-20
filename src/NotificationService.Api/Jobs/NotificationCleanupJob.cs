using Quartz;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Services.Delivery;

namespace NotificationService.Api.Jobs;

/// <summary>
/// Background job that cleans up old notifications and delivery records.
/// Runs daily at 2 AM to:
/// - Expire old notifications
/// - Delete acknowledged notifications
/// - Fix stale delivery records (DeliveredAt set but Status=Pending)
/// - Delete old delivered delivery records
/// </summary>
public class NotificationCleanupJob : IJob
{
    private readonly INotificationService _notificationService;
    private readonly IDeliveryTrackingService _deliveryTrackingService;
    private readonly ILogger<NotificationCleanupJob> _logger;

    public NotificationCleanupJob(
        INotificationService notificationService,
        IDeliveryTrackingService deliveryTrackingService,
        ILogger<NotificationCleanupJob> logger)
    {
        _notificationService = notificationService;
        _deliveryTrackingService = deliveryTrackingService;
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

            // Fix stale delivery records (DeliveredAt set but Status still Pending)
            // This handles records created before the bug fix
            var fixedCount = await _deliveryTrackingService.FixStaleDeliveredRecordsAsync();
            if (fixedCount > 0)
            {
                _logger.LogInformation("Fixed {Count} stale delivery records", fixedCount);
            }

            // Delete old delivered delivery records > 30 days to prevent queue bloat
            var deletedCount = await _deliveryTrackingService.DeleteOldDeliveredAsync(daysOld: 30);
            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} old delivered records", deletedCount);
            }

            _logger.LogInformation("Notification cleanup job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification cleanup job");
            throw;
        }
    }
}
