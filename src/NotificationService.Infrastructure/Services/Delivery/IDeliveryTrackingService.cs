using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Delivery;

/// <summary>
/// Service for tracking notification deliveries across channels
/// </summary>
public interface IDeliveryTrackingService
{
    /// <summary>
    /// Get all queued deliveries (Pending or Failed with retry scheduled)
    /// </summary>
    Task<List<NotificationDelivery>> GetQueuedDeliveriesAsync();

    /// <summary>
    /// Get delivery history with optional filtering
    /// </summary>
    Task<List<NotificationDelivery>> GetDeliveryHistoryAsync(
        NotificationChannel? channel = null,
        DeliveryStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get delivery statistics
    /// </summary>
    Task<DeliveryStatistics> GetDeliveryStatsAsync();

    /// <summary>
    /// Retry a failed delivery
    /// </summary>
    Task RetryDeliveryAsync(Guid deliveryId);

    /// <summary>
    /// Cancel a pending delivery
    /// </summary>
    Task CancelDeliveryAsync(Guid deliveryId);

    /// <summary>
    /// Delete old delivered notification deliveries to prevent queue bloat.
    /// Returns the number of records deleted.
    /// </summary>
    Task<int> DeleteOldDeliveredAsync(int daysOld = 30);

    /// <summary>
    /// Fix stale delivery records that have DeliveredAt set but Status is still Pending.
    /// This corrects records created before the Status field was properly set.
    /// Returns the number of records fixed.
    /// </summary>
    Task<int> FixStaleDeliveredRecordsAsync();

    /// <summary>
    /// Get all deliveries (queue and history) for a specific notification.
    /// Used for the delivery timeline view.
    /// </summary>
    Task<NotificationDeliveriesResult> GetDeliveriesByNotificationAsync(Guid notificationId);

    /// <summary>
    /// Re-queue a notification for delivery.
    /// Creates new delivery records for all configured channels.
    /// </summary>
    Task RequeueNotificationAsync(Guid notificationId);
}

/// <summary>
/// Result containing all deliveries for a notification
/// </summary>
public class NotificationDeliveriesResult
{
    public Guid NotificationId { get; set; }
    public List<NotificationDelivery> Deliveries { get; set; } = new();
    public List<NotificationDelivery> QueuedItems { get; set; } = new();
}

/// <summary>
/// Delivery statistics model
/// </summary>
public class DeliveryStatistics
{
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalPending { get; set; }
    public double DeliveryRatePercent { get; set; }
    public double AvgDeliveryTimeMs { get; set; }
    public List<ChannelStatistics> ChannelStats { get; set; } = new();
}

public class ChannelStatistics
{
    public NotificationChannel Channel { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
}
