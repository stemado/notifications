using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
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
    private readonly IChannelConfigurationService _channelConfig;
    private readonly ILogger<ChannelsController> _logger;

    public ChannelsController(
        IChannelHealthService channelHealth,
        IChannelConfigurationService channelConfig,
        ILogger<ChannelsController> logger)
    {
        _channelHealth = channelHealth;
        _channelConfig = channelConfig;
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

    /// <summary>
    /// Get all channel configurations
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<IEnumerable<ChannelConfigurationResponse>>> GetAllConfigurations()
    {
        try
        {
            var configs = await _channelConfig.GetAllConfigurationsAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving channel configurations");
            return StatusCode(500, new { error = "Failed to retrieve channel configurations" });
        }
    }

    /// <summary>
    /// Get configuration for a specific channel
    /// </summary>
    [HttpGet("{channel}/config")]
    public async Task<ActionResult<ChannelConfigurationResponse>> GetChannelConfiguration(string channel)
    {
        try
        {
            if (!Enum.TryParse<NotificationChannel>(channel, true, out var channelEnum))
            {
                return BadRequest(new { error = $"Invalid channel: {channel}" });
            }

            var config = await _channelConfig.GetConfigurationAsync(channelEnum);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration for channel {Channel}", channel);
            return StatusCode(500, new { error = "Failed to retrieve channel configuration" });
        }
    }

    /// <summary>
    /// Update configuration for a specific channel
    /// </summary>
    [HttpPut("{channel}/config")]
    public async Task<ActionResult<ChannelConfigurationResponse>> UpdateChannelConfiguration(
        string channel,
        [FromBody] UpdateChannelConfigurationRequest request)
    {
        try
        {
            if (!Enum.TryParse<NotificationChannel>(channel, true, out var channelEnum))
            {
                return BadRequest(new { error = $"Invalid channel: {channel}" });
            }

            var config = await _channelConfig.UpdateConfigurationAsync(channelEnum, request);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration for channel {Channel}", channel);
            return StatusCode(500, new { error = "Failed to update channel configuration" });
        }
    }

    /// <summary>
    /// Enable a channel
    /// </summary>
    [HttpPost("{channel}/enable")]
    public async Task<ActionResult> EnableChannel(string channel)
    {
        try
        {
            if (!Enum.TryParse<NotificationChannel>(channel, true, out var channelEnum))
            {
                return BadRequest(new { error = $"Invalid channel: {channel}" });
            }

            await _channelConfig.EnableChannelAsync(channelEnum);
            return Ok(new { message = $"Channel {channel} enabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling channel {Channel}", channel);
            return StatusCode(500, new { error = "Failed to enable channel" });
        }
    }

    /// <summary>
    /// Disable a channel
    /// </summary>
    [HttpPost("{channel}/disable")]
    public async Task<ActionResult> DisableChannel(string channel)
    {
        try
        {
            if (!Enum.TryParse<NotificationChannel>(channel, true, out var channelEnum))
            {
                return BadRequest(new { error = $"Invalid channel: {channel}" });
            }

            await _channelConfig.DisableChannelAsync(channelEnum);
            return Ok(new { message = $"Channel {channel} disabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling channel {Channel}", channel);
            return StatusCode(500, new { error = "Failed to disable channel" });
        }
    }

    /// <summary>
    /// Test a channel's connectivity
    /// </summary>
    [HttpPost("{channel}/test")]
    public async Task<ActionResult<TestChannelResult>> TestChannel(string channel)
    {
        try
        {
            if (!Enum.TryParse<NotificationChannel>(channel, true, out var channelEnum))
            {
                return BadRequest(new { error = $"Invalid channel: {channel}" });
            }

            var result = await _channelConfig.TestChannelAsync(channelEnum);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing channel {Channel}", channel);
            return StatusCode(500, new { error = "Failed to test channel" });
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
