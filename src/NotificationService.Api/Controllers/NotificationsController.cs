using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

/// <summary>
/// API controller for notification management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active notifications for the current user
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<Notification>>> GetActiveNotifications()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            var notifications = await _notificationService.GetActiveForUserAsync(userId.Value);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active notifications for user {UserId}", userId);
            return StatusCode(500, "Error retrieving notifications");
        }
    }

    /// <summary>
    /// Gets a specific notification by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> GetNotification(Guid id)
    {
        try
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification == null)
            {
                return NotFound($"Notification {id} not found");
            }

            // Check authorization
            var userId = GetCurrentUserId();
            if (userId != null && notification.UserId != userId.Value)
            {
                return Forbid();
            }

            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId}", id);
            return StatusCode(500, "Error retrieving notification");
        }
    }

    /// <summary>
    /// Gets all notifications for a tenant (admin/ops only)
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<List<Notification>>> GetTenantNotifications(Guid tenantId)
    {
        // Phase 2: Add authorization check for admin/ops role

        try
        {
            var notifications = await _notificationService.GetByTenantAsync(tenantId);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for tenant {TenantId}", tenantId);
            return StatusCode(500, "Error retrieving tenant notifications");
        }
    }

    /// <summary>
    /// Creates a new notification and dispatches it to all enabled channels
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = await _notificationService.CreateAsync(request);

            // Dispatch to all enabled channels (SignalR, Email, SMS, etc.) with delivery tracking
            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation("Created and dispatched notification {NotificationId} for user {UserId}",
                notification.Id, notification.UserId);

            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, "Error creating notification");
        }
    }

    /// <summary>
    /// Creates or updates a notification using GroupKey and dispatches to all enabled channels
    /// </summary>
    [HttpPost("create-or-update")]
    public async Task<ActionResult<Notification>> CreateOrUpdateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = await _notificationService.CreateOrUpdateAsync(request);

            // Dispatch to all enabled channels (SignalR, Email, SMS, etc.) with delivery tracking
            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation("Created or updated and dispatched notification {NotificationId} for user {UserId}",
                notification.Id, notification.UserId);

            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating notification");
            return StatusCode(500, "Error creating or updating notification");
        }
    }

    /// <summary>
    /// Acknowledges a notification
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    public async Task<ActionResult> AcknowledgeNotification(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            await _notificationService.AcknowledgeAsync(id, userId.Value);
            _logger.LogInformation("User {UserId} acknowledged notification {NotificationId}", userId, id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging notification {NotificationId}", id);
            return StatusCode(500, "Error acknowledging notification");
        }
    }

    /// <summary>
    /// Dismisses a notification
    /// </summary>
    [HttpPost("{id}/dismiss")]
    public async Task<ActionResult> DismissNotification(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            await _notificationService.DismissAsync(id, userId.Value);
            _logger.LogInformation("User {UserId} dismissed notification {NotificationId}", userId, id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing notification {NotificationId}", id);
            return StatusCode(500, "Error dismissing notification");
        }
    }

    /// <summary>
    /// Snoozes a notification for specified minutes
    /// </summary>
    [HttpPost("{id}/snooze")]
    public async Task<ActionResult> SnoozeNotification(Guid id, [FromQuery] int minutes)
    {
        if (minutes <= 0)
        {
            return BadRequest("Minutes must be greater than 0");
        }

        try
        {
            await _notificationService.SnoozeAsync(id, minutes);
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} snoozed notification {NotificationId} for {Minutes} minutes",
                userId, id, minutes);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error snoozing notification {NotificationId}", id);
            return StatusCode(500, "Error snoozing notification");
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
