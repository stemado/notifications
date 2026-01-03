using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Services.Delivery;

/// <summary>
/// Implementation of delivery tracking service
/// </summary>
public class DeliveryTrackingService : IDeliveryTrackingService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<DeliveryTrackingService> _logger;

    public DeliveryTrackingService(
        NotificationDbContext context,
        ILogger<DeliveryTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<NotificationDelivery>> GetQueuedDeliveriesAsync()
    {
        return await _context.NotificationDeliveries
            .Where(d => d.Status == DeliveryStatus.Pending || d.Status == DeliveryStatus.Failed)
            .Where(d => d.NextRetryAt == null || d.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<NotificationDelivery>> GetDeliveryHistoryAsync(
        NotificationChannel? channel = null,
        DeliveryStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.NotificationDeliveries.AsQueryable();

        if (channel.HasValue)
            query = query.Where(d => d.Channel == channel.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<DeliveryStatistics> GetDeliveryStatsAsync()
    {
        var deliveries = await _context.NotificationDeliveries
            .Where(d => d.CreatedAt >= DateTime.UtcNow.AddDays(-30)) // Last 30 days
            .ToListAsync();

        var totalDelivered = deliveries.Count(d => d.Status == DeliveryStatus.Delivered);
        var totalFailed = deliveries.Count(d => d.Status == DeliveryStatus.Failed || d.Status == DeliveryStatus.Bounced);
        var totalPending = deliveries.Count(d => d.Status == DeliveryStatus.Pending || d.Status == DeliveryStatus.Processing);

        var totalAttempted = totalDelivered + totalFailed;
        var deliveryRate = totalAttempted > 0 ? (double)totalDelivered / totalAttempted * 100 : 0;

        // Calculate average delivery time for successful deliveries
        var successfulWithTime = deliveries
            .Where(d => d.Status == DeliveryStatus.Delivered && d.DeliveredAt.HasValue)
            .ToList();

        var avgDeliveryTime = successfulWithTime.Any()
            ? successfulWithTime.Average(d => (d.DeliveredAt!.Value - d.CreatedAt).TotalMilliseconds)
            : 0;

        // Per-channel statistics
        var channelStats = deliveries
            .GroupBy(d => d.Channel)
            .Select(g => new ChannelStatistics
            {
                Channel = g.Key,
                Delivered = g.Count(d => d.Status == DeliveryStatus.Delivered),
                Failed = g.Count(d => d.Status == DeliveryStatus.Failed || d.Status == DeliveryStatus.Bounced),
                Pending = g.Count(d => d.Status == DeliveryStatus.Pending || d.Status == DeliveryStatus.Processing)
            })
            .ToList();

        return new DeliveryStatistics
        {
            TotalDelivered = totalDelivered,
            TotalFailed = totalFailed,
            TotalPending = totalPending,
            DeliveryRatePercent = deliveryRate,
            AvgDeliveryTimeMs = avgDeliveryTime,
            ChannelStats = channelStats
        };
    }

    public async Task RetryDeliveryAsync(Guid deliveryId)
    {
        var delivery = await _context.NotificationDeliveries.FindAsync(deliveryId);
        if (delivery == null)
        {
            _logger.LogWarning("Delivery {DeliveryId} not found for retry", deliveryId);
            throw new InvalidOperationException($"Delivery {deliveryId} not found");
        }

        if (delivery.Status != DeliveryStatus.Failed)
        {
            _logger.LogWarning("Attempted to retry delivery {DeliveryId} with status {Status}", deliveryId, delivery.Status);
            throw new InvalidOperationException($"Can only retry failed deliveries. Current status: {delivery.Status}");
        }

        delivery.Status = DeliveryStatus.Pending;
        delivery.NextRetryAt = null; // Clear retry schedule
        delivery.ErrorMessage = null;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Delivery {DeliveryId} queued for retry", deliveryId);
    }

    public async Task CancelDeliveryAsync(Guid deliveryId)
    {
        var delivery = await _context.NotificationDeliveries.FindAsync(deliveryId);
        if (delivery == null)
        {
            _logger.LogWarning("Delivery {DeliveryId} not found for cancellation", deliveryId);
            throw new InvalidOperationException($"Delivery {deliveryId} not found");
        }

        if (delivery.Status != DeliveryStatus.Pending && delivery.Status != DeliveryStatus.Failed)
        {
            _logger.LogWarning("Attempted to cancel delivery {DeliveryId} with status {Status}", deliveryId, delivery.Status);
            throw new InvalidOperationException($"Can only cancel pending or failed deliveries. Current status: {delivery.Status}");
        }

        delivery.Status = DeliveryStatus.Cancelled;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Delivery {DeliveryId} cancelled", deliveryId);
    }

    public async Task<int> DeleteOldDeliveredAsync(int daysOld = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var oldDeliveries = await _context.NotificationDeliveries
            .Where(d => d.Status == DeliveryStatus.Delivered)
            .Where(d => d.DeliveredAt != null && d.DeliveredAt < cutoffDate)
            .ToListAsync();

        if (oldDeliveries.Count == 0)
        {
            _logger.LogDebug("No old delivered records to clean up");
            return 0;
        }

        _context.NotificationDeliveries.RemoveRange(oldDeliveries);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted {Count} old delivered notification deliveries older than {DaysOld} days",
            oldDeliveries.Count, daysOld);

        return oldDeliveries.Count;
    }

    public async Task<int> FixStaleDeliveredRecordsAsync()
    {
        // Find records where delivery succeeded (DeliveredAt is set) but Status is still Pending
        var staleRecords = await _context.NotificationDeliveries
            .Where(d => d.Status == DeliveryStatus.Pending && d.DeliveredAt != null)
            .ToListAsync();

        if (staleRecords.Count == 0)
        {
            _logger.LogDebug("No stale delivered records to fix");
            return 0;
        }

        foreach (var record in staleRecords)
        {
            record.Status = DeliveryStatus.Delivered;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Fixed {Count} stale delivery records (had DeliveredAt but Status was Pending)",
            staleRecords.Count);

        return staleRecords.Count;
    }

    public async Task<NotificationDeliveriesResult> GetDeliveriesByNotificationAsync(Guid notificationId)
    {
        var allDeliveries = await _context.NotificationDeliveries
            .Where(d => d.NotificationId == notificationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        // Split into completed deliveries (history) and queued items
        var completedStatuses = new[] { DeliveryStatus.Delivered, DeliveryStatus.Failed, DeliveryStatus.Bounced, DeliveryStatus.Cancelled };
        var queuedStatuses = new[] { DeliveryStatus.Pending, DeliveryStatus.Processing };

        return new NotificationDeliveriesResult
        {
            NotificationId = notificationId,
            Deliveries = allDeliveries.Where(d => completedStatuses.Contains(d.Status)).ToList(),
            QueuedItems = allDeliveries.Where(d => queuedStatuses.Contains(d.Status)).ToList()
        };
    }

    public async Task RequeueNotificationAsync(Guid notificationId)
    {
        // Verify the notification exists
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found for requeue", notificationId);
            throw new InvalidOperationException($"Notification {notificationId} not found");
        }

        // Get existing failed deliveries for this notification to determine which channels to retry
        var failedDeliveries = await _context.NotificationDeliveries
            .Where(d => d.NotificationId == notificationId)
            .Where(d => d.Status == DeliveryStatus.Failed || d.Status == DeliveryStatus.Bounced)
            .ToListAsync();

        if (!failedDeliveries.Any())
        {
            _logger.LogWarning("No failed deliveries found for notification {NotificationId}", notificationId);
            throw new InvalidOperationException($"No failed deliveries to requeue for notification {notificationId}");
        }

        // Create new pending delivery records for each failed channel
        foreach (var failed in failedDeliveries)
        {
            var newDelivery = new NotificationDelivery
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                Channel = failed.Channel,
                Status = DeliveryStatus.Pending,
                AttemptCount = 0,
                MaxAttempts = failed.MaxAttempts,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationDeliveries.Add(newDelivery);
            _logger.LogInformation(
                "Created new delivery {DeliveryId} for notification {NotificationId} on channel {Channel}",
                newDelivery.Id, notificationId, failed.Channel);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Requeued {Count} channels for notification {NotificationId}",
            failedDeliveries.Count, notificationId);
    }
}
