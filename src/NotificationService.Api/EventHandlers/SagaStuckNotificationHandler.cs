using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Events;
using NotificationService.Api.Hubs;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles SagaStuckEvent by creating notifications and pushing them via SignalR
/// </summary>
public class SagaStuckNotificationHandler : IEventHandler<SagaStuckEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SagaStuckNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public SagaStuckNotificationHandler(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<SagaStuckNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(SagaStuckEvent evt)
    {
        try
        {
            var severity = DetermineSeverity(evt.StuckDuration);
            var repeatInterval = DetermineRepeatInterval(severity);

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                // Ownership (Phase 1: All ops users, Phase 2: evt.TenantId when client portals exist)
                UserId = GetOpsTeamUserId(),
                TenantId = null, // Phase 2: evt.TenantId when client portals exist

                // Content
                Severity = severity,
                Title = "Saga Stuck",
                Message = $"{evt.ClientName} has been stuck for {FormatDuration(evt.StuckDuration)}",

                // Source (event sourcing ready)
                SagaId = evt.SagaId,
                ClientId = evt.ClientId,
                EventId = evt.Id, // Link to domain event
                EventType = nameof(SagaStuckEvent),

                // Behavior
                RepeatInterval = repeatInterval,
                RequiresAck = severity >= NotificationSeverity.Urgent,
                ExpiresAt = severity >= NotificationSeverity.Urgent ? null : DateTime.UtcNow.AddDays(3),

                // Grouping (prevents duplicates)
                GroupKey = $"saga:stuck:{evt.SagaId}",

                // Actions
                Actions = new List<NotificationAction>
                {
                    new() { Label = "Fix Now", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" },
                    new() { Label = "Snooze 1h", Action = "dismiss", Variant = "secondary" }
                }
            });

            // Push via SignalR (Phase 1)
            await _hubContext.Clients.Group("ops-team").SendAsync("NewNotification", notification);

            _logger.LogInformation(
                "Created notification {NotificationId} for stuck saga {SagaId} with severity {Severity}",
                notification.Id, evt.SagaId, severity);

            // Future: Multi-channel dispatch (Phase 2)
            // await _notificationDispatcher.DispatchAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SagaStuckEvent for saga {SagaId}", evt.SagaId);
            throw;
        }
    }

    private NotificationSeverity DetermineSeverity(TimeSpan duration)
    {
        if (duration > TimeSpan.FromDays(3)) return NotificationSeverity.Critical;
        if (duration > TimeSpan.FromHours(24)) return NotificationSeverity.Urgent;
        if (duration > TimeSpan.FromHours(2)) return NotificationSeverity.Warning;
        return NotificationSeverity.Info;
    }

    private int? DetermineRepeatInterval(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => 15,  // Every 15 minutes
            NotificationSeverity.Urgent => 30,    // Every 30 minutes
            _ => null                             // No repeat
        };
    }

    private Guid GetOpsTeamUserId()
    {
        // Phase 1: Return a configured ops team user ID
        // In production, this would query all ops team members
        var opsUserId = _configuration["Notifications:OpsTeamUserId"];
        if (!string.IsNullOrEmpty(opsUserId) && Guid.TryParse(opsUserId, out var userId))
        {
            return userId;
        }

        // Fallback for development
        _logger.LogWarning("No OpsTeamUserId configured, using default");
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays} day(s)";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} hour(s)";
        return $"{(int)duration.TotalMinutes} minute(s)";
    }
}
