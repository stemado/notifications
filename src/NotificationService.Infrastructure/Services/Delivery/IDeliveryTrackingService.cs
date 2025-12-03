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
