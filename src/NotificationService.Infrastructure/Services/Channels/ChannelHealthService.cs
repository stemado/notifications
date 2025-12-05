using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Implementation of channel health monitoring service
/// </summary>
public class ChannelHealthService : IChannelHealthService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<ChannelHealthService> _logger;

    public ChannelHealthService(
        NotificationDbContext context,
        ILogger<ChannelHealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets channel configuration from database, returns defaults if not found
    /// </summary>
    private async Task<(bool IsEnabled, bool IsConfigured)> GetChannelConfigurationAsync(NotificationChannel channel)
    {
        var config = await _context.ChannelConfigurations
            .FirstOrDefaultAsync(c => c.Channel == channel);

        if (config != null)
        {
            return (config.Enabled, config.Configured);
        }

        // Default: SignalR is always configured/enabled, others are not
        return channel == NotificationChannel.SignalR
            ? (true, true)
            : (false, false);
    }

    public async Task<List<ChannelHealth>> GetAllChannelHealthAsync()
    {
        var allChannels = Enum.GetValues<NotificationChannel>();
        var healthStatuses = new List<ChannelHealth>();

        foreach (var channel in allChannels)
        {
            var health = await GetChannelHealthAsync(channel);
            healthStatuses.Add(health);
        }

        return healthStatuses;
    }

    public async Task<ChannelHealth> GetChannelHealthAsync(NotificationChannel channel)
    {
        var cutoff24h = DateTime.UtcNow.AddHours(-24);

        // Get recent delivery statistics for this channel
        var recentDeliveries = await _context.NotificationDeliveries
            .Where(d => d.Channel == channel && d.CreatedAt >= cutoff24h)
            .ToListAsync();

        var lastSuccessfulDelivery = await _context.NotificationDeliveries
            .Where(d => d.Channel == channel && d.Status == DeliveryStatus.Delivered)
            .OrderByDescending(d => d.DeliveredAt)
            .Select(d => d.DeliveredAt)
            .FirstOrDefaultAsync();

        var errorCount = recentDeliveries.Count(d =>
            d.Status == DeliveryStatus.Failed || d.Status == DeliveryStatus.Bounced);

        var totalCount = recentDeliveries.Count;
        var successCount = recentDeliveries.Count(d => d.Status == DeliveryStatus.Delivered);

        // Get channel configuration from database
        var (isEnabled, isConfigured) = await GetChannelConfigurationAsync(channel);

        // Determine health status
        var status = DetermineHealthStatus(totalCount, successCount, errorCount, lastSuccessfulDelivery, isConfigured);

        return new ChannelHealth
        {
            Channel = channel,
            Status = status,
            LastDeliveryAt = lastSuccessfulDelivery,
            ErrorCount24h = errorCount,
            Enabled = isEnabled,
            Configured = isConfigured,
            Config = await GetChannelConfigAsync(channel)
        };
    }

    private static ChannelHealthStatus DetermineHealthStatus(
        int totalCount,
        int successCount,
        int errorCount,
        DateTime? lastSuccessfulDelivery,
        bool isConfigured)
    {
        // If no deliveries attempted, status depends on configuration and last successful delivery
        if (totalCount == 0)
        {
            // If we have a recent successful delivery, we're healthy
            if (lastSuccessfulDelivery.HasValue && lastSuccessfulDelivery.Value > DateTime.UtcNow.AddHours(-48))
                return ChannelHealthStatus.Healthy;

            // If we have an old successful delivery, we're degraded
            if (lastSuccessfulDelivery.HasValue)
                return ChannelHealthStatus.Degraded;

            // If configured and enabled but no delivery history (fresh deployment), consider healthy
            if (isConfigured)
                return ChannelHealthStatus.Healthy;

            // Not configured and no history = unhealthy
            return ChannelHealthStatus.Unhealthy;
        }

        var successRate = (double)successCount / totalCount;

        // Healthy: > 95% success rate
        if (successRate > 0.95)
            return ChannelHealthStatus.Healthy;

        // Degraded: 70-95% success rate
        if (successRate > 0.70)
            return ChannelHealthStatus.Degraded;

        // Unhealthy: < 70% success rate
        return ChannelHealthStatus.Unhealthy;
    }

    /// <summary>
    /// Gets sanitized configuration info from database (no secrets)
    /// </summary>
    private async Task<Dictionary<string, object>?> GetChannelConfigAsync(NotificationChannel channel)
    {
        var config = await _context.ChannelConfigurations
            .FirstOrDefaultAsync(c => c.Channel == channel);

        if (config == null || string.IsNullOrEmpty(config.ConfigurationJson))
        {
            // Return default config for SignalR (always configured)
            if (channel == NotificationChannel.SignalR)
            {
                return new Dictionary<string, object>
                {
                    ["type"] = "in-memory",
                    ["hubPath"] = "/notificationHub"
                };
            }
            return null;
        }

        try
        {
            // Parse the stored JSON and return sanitized version
            var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(config.ConfigurationJson);

            // Return sanitized config based on channel type
            return channel switch
            {
                NotificationChannel.SignalR => new Dictionary<string, object>
                {
                    ["type"] = "in-memory",
                    ["hubPath"] = "/notificationHub"
                },
                NotificationChannel.Email => config.Configured ? new Dictionary<string, object>
                {
                    ["provider"] = configDict?.GetValueOrDefault("provider")?.ToString() ?? "SMTP",
                    ["configured"] = true
                } : null,
                NotificationChannel.SMS => config.Configured ? new Dictionary<string, object>
                {
                    ["provider"] = "Twilio",
                    ["configured"] = true
                } : null,
                NotificationChannel.Teams => config.Configured ? new Dictionary<string, object>
                {
                    ["provider"] = "WebhookConnector",
                    ["configured"] = true
                } : null,
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse configuration for channel {Channel}", channel);
            return null;
        }
    }
}
