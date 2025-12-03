using Microsoft.AspNetCore.Mvc;
using NotificationService.Infrastructure.Services.Channels;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for notification channel status and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelHealthService _channelHealth;
    private readonly ILogger<ChannelsController> _logger;

    public ChannelsController(
        IChannelHealthService channelHealth,
        ILogger<ChannelsController> logger)
    {
        _channelHealth = channelHealth;
        _logger = logger;
    }

    /// <summary>
    /// Get status of all notification channels
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetChannelStatus()
    {
        try
        {
            var healthStatuses = await _channelHealth.GetAllChannelHealthAsync();

            // Map to frontend-expected format
            var response = healthStatuses.Select(h => new
            {
                channel = h.Channel.ToString(),
                enabled = h.Enabled,
                configured = h.Configured,
                status = MapHealthStatus(h.Status),
                lastDeliveryAt = h.LastDeliveryAt?.ToString("o"),
                errorCount24h = h.ErrorCount24h,
                config = h.Config
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving channel status");
            return StatusCode(500, new { error = "Failed to retrieve channel status" });
        }
    }

    private static string MapHealthStatus(ChannelHealthStatus status)
    {
        return status switch
        {
            ChannelHealthStatus.Healthy => "healthy",
            ChannelHealthStatus.Degraded => "degraded",
            ChannelHealthStatus.Unhealthy => "unhealthy",
            _ => "unhealthy"
        };
    }
}
