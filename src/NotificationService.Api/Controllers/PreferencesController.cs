using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models.Preferences;
using NotificationService.Infrastructure.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

/// <summary>
/// API controller for user notification preferences (Phase 2)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PreferencesController : ControllerBase
{
    private readonly IUserPreferenceService _preferenceService;
    private readonly ILogger<PreferencesController> _logger;

    public PreferencesController(
        IUserPreferenceService preferenceService,
        ILogger<PreferencesController> logger)
    {
        _preferenceService = preferenceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all preferences for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserNotificationPreference>>> GetPreferences()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            var preferences = await _preferenceService.GetUserPreferencesAsync(userId.Value);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user {UserId}", userId);
            return StatusCode(500, "Error retrieving preferences");
        }
    }

    /// <summary>
    /// Gets preference for a specific channel
    /// </summary>
    [HttpGet("{channel}")]
    public async Task<ActionResult<UserNotificationPreference>> GetPreference(NotificationChannel channel)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            var preference = await _preferenceService.GetPreferenceAsync(userId.Value, channel);
            if (preference == null)
            {
                return NotFound($"Preference for channel {channel} not found");
            }
            return Ok(preference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preference for user {UserId} and channel {Channel}", userId, channel);
            return StatusCode(500, "Error retrieving preference");
        }
    }

    /// <summary>
    /// Sets or updates a preference
    /// </summary>
    [HttpPut("{channel}")]
    public async Task<ActionResult<UserNotificationPreference>> SetPreference(
        NotificationChannel channel,
        [FromBody] SetPreferenceRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            var preference = await _preferenceService.SetPreferenceAsync(
                userId.Value,
                channel,
                request.MinSeverity,
                request.Enabled);

            _logger.LogInformation(
                "User {UserId} updated preference for channel {Channel}",
                userId, channel);

            return Ok(preference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting preference for user {UserId} and channel {Channel}", userId, channel);
            return StatusCode(500, "Error setting preference");
        }
    }

    /// <summary>
    /// Deletes a preference (resets to default)
    /// </summary>
    [HttpDelete("{channel}")]
    public async Task<ActionResult> DeletePreference(NotificationChannel channel)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            await _preferenceService.DeletePreferenceAsync(userId.Value, channel);
            _logger.LogInformation(
                "User {UserId} deleted preference for channel {Channel}",
                userId, channel);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting preference for user {UserId} and channel {Channel}", userId, channel);
            return StatusCode(500, "Error deleting preference");
        }
    }

    /// <summary>
    /// Sets default preferences for the current user
    /// </summary>
    [HttpPost("defaults")]
    public async Task<ActionResult> SetDefaultPreferences()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            await _preferenceService.SetDefaultPreferencesAsync(userId.Value);
            _logger.LogInformation("Set default preferences for user {UserId}", userId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default preferences for user {UserId}", userId);
            return StatusCode(500, "Error setting default preferences");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Request model for setting a preference
/// </summary>
public class SetPreferenceRequest
{
    public NotificationSeverity MinSeverity { get; set; }
    public bool Enabled { get; set; }
}
