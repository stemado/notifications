using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Health check controller for monitoring service status
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Get overall service health status
    /// </summary>
    /// <returns>Health status with component details</returns>
    [HttpGet]
    public async Task<ActionResult<ServiceHealthResponse>> GetHealth()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new ServiceHealthResponse
            {
                Status = report.Status.ToString(),
                LastCheck = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                Components = report.Entries.Select(e => new ComponentHealth
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    DurationMs = e.Value.Duration.TotalMilliseconds,
                    Error = e.Value.Exception?.Message
                }).ToList(),
                TotalDurationMs = report.TotalDuration.TotalMilliseconds
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
            return StatusCode(503, new ServiceHealthResponse
            {
                Status = "Unhealthy",
                LastCheck = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                Components = new List<ComponentHealth>
                {
                    new()
                    {
                        Name = "health-check",
                        Status = "Unhealthy",
                        Error = ex.Message
                    }
                }
            });
        }
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

/// <summary>
/// Response model for service health
/// </summary>
public class ServiceHealthResponse
{
    public string Status { get; set; } = "Unknown";
    public DateTime LastCheck { get; set; }
    public string Version { get; set; } = "1.0.0";
    public List<ComponentHealth> Components { get; set; } = new();
    public double TotalDurationMs { get; set; }
}

/// <summary>
/// Health status for individual components
/// </summary>
public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public string? Description { get; set; }
    public double DurationMs { get; set; }
    public string? Error { get; set; }
}
