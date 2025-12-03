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

        // Determine health status
        var status = DetermineHealthStatus(totalCount, successCount, errorCount, lastSuccessfulDelivery);

        // Check if channel is configured (this would come from configuration in a real implementation)
        var isConfigured = IsChannelConfigured(channel);
        var isEnabled = IsChannelEnabled(channel);

        return new ChannelHealth
        {
            Channel = channel,
            Status = status,
            LastDeliveryAt = lastSuccessfulDelivery,
            ErrorCount24h = errorCount,
            Enabled = isEnabled,
            Configured = isConfigured,
            Config = GetChannelConfig(channel)
        };
    }

    private static ChannelHealthStatus DetermineHealthStatus(
        int totalCount,
        int successCount,
        int errorCount,
        DateTime? lastSuccessfulDelivery)
    {
        // If no deliveries attempted, status depends on last successful delivery
        if (totalCount == 0)
        {
            if (lastSuccessfulDelivery.HasValue && lastSuccessfulDelivery.Value > DateTime.UtcNow.AddHours(-48))
                return ChannelHealthStatus.Healthy;
            if (lastSuccessfulDelivery.HasValue)
                return ChannelHealthStatus.Degraded;
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

    private static bool IsChannelConfigured(NotificationChannel channel)
    {
        // In a real implementation, this would check configuration
        // For now, SignalR is always configured (in-memory), others depend on env vars
        return channel switch
        {
            NotificationChannel.SignalR => true,
            NotificationChannel.Email => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMTP_HOST")),
            NotificationChannel.SMS => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")),
            NotificationChannel.Teams => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMS_WEBHOOK_URL")),
            _ => false
        };
    }

    private static bool IsChannelEnabled(NotificationChannel channel)
    {
        // In a real implementation, this would check feature flags or configuration
        // For now, all configured channels are enabled
        return IsChannelConfigured(channel);
    }

    private static Dictionary<string, object>? GetChannelConfig(NotificationChannel channel)
    {
        // Return sanitized configuration info (no secrets)
        return channel switch
        {
            NotificationChannel.SignalR => new Dictionary<string, object>
            {
                ["type"] = "in-memory",
                ["hubPath"] = "/notificationHub"
            },
            NotificationChannel.Email => IsChannelConfigured(NotificationChannel.Email) ? new Dictionary<string, object>
            {
                ["provider"] = "SMTP",
                ["host"] = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "not-configured"
            } : null,
            NotificationChannel.SMS => IsChannelConfigured(NotificationChannel.SMS) ? new Dictionary<string, object>
            {
                ["provider"] = "Twilio"
            } : null,
            NotificationChannel.Teams => IsChannelConfigured(NotificationChannel.Teams) ? new Dictionary<string, object>
            {
                ["provider"] = "WebhookConnector"
            } : null,
            _ => null
        };
    }
}
