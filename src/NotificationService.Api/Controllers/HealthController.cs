using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotificationService.Infrastructure.Services.Channels;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Health check controller for monitoring service status
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IChannelHealthService _channelHealth;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        IChannelHealthService channelHealth,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _channelHealth = channelHealth;
        _logger = logger;
    }

    /// <summary>
    /// Get overall service health status
    /// </summary>
    /// <returns>Health status with component details</returns>
    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();

            // Calculate uptime
            var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
            var uptime = DateTime.Now - startTime;
            var uptimeString = $"{(int)uptime.TotalHours}h {uptime.Minutes}m";

            // Get channel health
            var channelHealthStatuses = await _channelHealth.GetAllChannelHealthAsync();
            var channels = channelHealthStatuses.Select(h => new
            {
                channel = h.Channel.ToString(),
                status = MapChannelHealthStatus(h.Status),
                lastDeliveryAt = h.LastDeliveryAt?.ToString("o"),
                errorCount24h = h.ErrorCount24h
            });

            // Return camelCase response matching frontend expectations
            var response = new
            {
                status = MapHealthStatus(report.Status),
                version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                uptime = uptimeString,
                lastCheck = DateTime.UtcNow.ToString("o"),
                channels
            };

            // Return appropriate status code based on health
            return report.Status switch
            {
                HealthStatus.Healthy => Ok(response),
                HealthStatus.Degraded => Ok(response), // Still 200 but with degraded status
                _ => StatusCode(503, response) // Service Unavailable for Unhealthy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service health");
            var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
            var uptime = DateTime.Now - startTime;
            return StatusCode(503, new
            {
                status = "unhealthy",
                version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                uptime = $"{(int)uptime.TotalHours}h {uptime.Minutes}m",
                lastCheck = DateTime.UtcNow.ToString("o"),
                channels = Array.Empty<object>()
            });
        }
    }

    private static string MapChannelHealthStatus(ChannelHealthStatus status)
    {
        return status switch
        {
            ChannelHealthStatus.Healthy => "healthy",
            ChannelHealthStatus.Degraded => "degraded",
            ChannelHealthStatus.Unhealthy => "unhealthy",
            _ => "unhealthy"
        };
    }

    private static string MapHealthStatus(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => "healthy",
            HealthStatus.Degraded => "degraded",
            HealthStatus.Unhealthy => "unhealthy",
            _ => "unhealthy"
        };
    }

    /// <summary>
    /// Simple liveness probe - returns 200 if service is running
    /// </summary>
    [HttpGet("live")]
    public ActionResult GetLiveness()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe - checks if service is ready to accept traffic
    /// </summary>
    [HttpGet("ready")]
    public async Task<ActionResult> GetReadiness()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();

            if (report.Status == HealthStatus.Healthy || report.Status == HealthStatus.Degraded)
            {
                return Ok(new { status = report.Status.ToString(), timestamp = DateTime.UtcNow });
            }

            return StatusCode(503, new { status = report.Status.ToString(), timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }
}
