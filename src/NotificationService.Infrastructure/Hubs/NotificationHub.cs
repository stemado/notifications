using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.Services;
using System.Security.Claims;

namespace NotificationService.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery
/// </summary>
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        INotificationService notificationService,
        ILogger<NotificationHub> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            _logger.LogWarning("Client connected without user ID");
            await base.OnConnectedAsync();
            return;
        }

        _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);

        // Add to user-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Add to tenant group (if applicable)
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (tenantId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
            _logger.LogInformation("User {UserId} added to tenant group: {TenantId}", userId, tenantId);
        }

        // Add to ops team group (if applicable)
        if (Context.User?.IsInRole("Operations") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "ops-team");
            _logger.LogInformation("User {UserId} added to ops-team group", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error", userId);
        }
        else
        {
            _logger.LogInformation("User {UserId} disconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client calls this to acknowledge a notification
    /// </summary>
    public async Task AcknowledgeNotification(Guid notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            await _notificationService.AcknowledgeAsync(notificationId, Guid.Parse(userId));
            _logger.LogInformation("User {UserId} acknowledged notification {NotificationId}", userId, notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new HubException($"Failed to acknowledge notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Client calls this to dismiss a notification
    /// </summary>
    public async Task DismissNotification(Guid notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            await _notificationService.DismissAsync(notificationId, Guid.Parse(userId));
            _logger.LogInformation("User {UserId} dismissed notification {NotificationId}", userId, notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new HubException($"Failed to dismiss notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Client calls this to snooze a notification
    /// </summary>
    public async Task SnoozeNotification(Guid notificationId, int minutes)
    {
        try
        {
            await _notificationService.SnoozeAsync(notificationId, minutes);
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} snoozed notification {NotificationId} for {Minutes} minutes",
                userId, notificationId, minutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error snoozing notification {NotificationId}", notificationId);
            throw new HubException($"Failed to snooze notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Client calls this to get their active notifications on connection
    /// </summary>
    public async Task<List<Domain.Models.Notification>> GetActiveNotifications()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            var notifications = await _notificationService.GetActiveForUserAsync(Guid.Parse(userId));
            _logger.LogInformation("User {UserId} retrieved {Count} active notifications", userId, notifications.Count);
            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active notifications for user {UserId}", userId);
            throw new HubException($"Failed to retrieve notifications: {ex.Message}");
        }
    }
}
