using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Service for monitoring channel health and availability
/// </summary>
public interface IChannelHealthService
{
    /// <summary>
    /// Get health status for all channels
    /// </summary>
    Task<List<ChannelHealth>> GetAllChannelHealthAsync();

    /// <summary>
    /// Get health status for a specific channel
    /// </summary>
    Task<ChannelHealth> GetChannelHealthAsync(NotificationChannel channel);
}

/// <summary>
/// Health information for a notification channel
/// </summary>
public class ChannelHealth
{
    public NotificationChannel Channel { get; set; }
    public ChannelHealthStatus Status { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
    public int ErrorCount24h { get; set; }
    public bool Enabled { get; set; }
    public bool Configured { get; set; }
    public Dictionary<string, object>? Config { get; set; }
}

/// <summary>
/// Channel health status
/// </summary>
public enum ChannelHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
