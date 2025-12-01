using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles EscalationCreatedEvent by creating notifications and dispatching them via multi-channel dispatcher.
/// Escalations require acknowledgment and may repeat based on severity.
/// </summary>
public class EscalationCreatedNotificationHandler : IEventHandler<EscalationCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<EscalationCreatedNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public EscalationCreatedNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<EscalationCreatedNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(EscalationCreatedEvent evt)
    {
        try
        {
            // Map client severity to domain severity
            var severity = MapSeverity(evt.Severity);
            var repeatInterval = DetermineRepeatInterval(severity);

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,

                Severity = severity,
                Title = $"Escalation: {evt.EscalationType} - {evt.ClientName}",
                Message = BuildMessage(evt),

                SagaId = evt.SagaId,
                ClientId = ParseGuidOrDefault(evt.ClientId),
                EventId = evt.EscalationId,
                EventType = nameof(EscalationCreatedEvent),

                RepeatInterval = repeatInterval,
                RequiresAck = true,
                GroupKey = $"escalation:{evt.EscalationId}",

                Actions = BuildActions(evt)
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogWarning(
                "Created and dispatched notification {NotificationId} for escalation {EscalationId} (Type: {EscalationType}, Severity: {Severity})",
                notification.Id, evt.EscalationId, evt.EscalationType, severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling EscalationCreatedEvent for escalation {EscalationId}", evt.EscalationId);
            throw;
        }
    }

    private NotificationSeverity MapSeverity(Client.Models.NotificationSeverity clientSeverity)
    {
        return clientSeverity switch
        {
            Client.Models.NotificationSeverity.Info => NotificationSeverity.Info,
            Client.Models.NotificationSeverity.Warning => NotificationSeverity.Warning,
            Client.Models.NotificationSeverity.Urgent => NotificationSeverity.Urgent,
            Client.Models.NotificationSeverity.Critical => NotificationSeverity.Critical,
            _ => NotificationSeverity.Warning
        };
    }

    private int? DetermineRepeatInterval(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => 15,  // Every 15 minutes
            NotificationSeverity.Urgent => 30,    // Every 30 minutes
            NotificationSeverity.Warning => 60,   // Every hour
            _ => null                             // No repeat
        };
    }

    private string BuildMessage(EscalationCreatedEvent evt)
    {
        var parts = new List<string> { evt.Reason };

        if (!string.IsNullOrEmpty(evt.CurrentState))
        {
            parts.Add($"Current state: {evt.CurrentState}");
        }

        if (evt.TimeInState > TimeSpan.Zero)
        {
            parts.Add($"Time in state: {FormatDuration(evt.TimeInState)}");
        }

        if (!string.IsNullOrEmpty(evt.FileName))
        {
            parts.Add($"File: {evt.FileName}");
        }

        return string.Join(". ", parts);
    }

    private List<NotificationAction> BuildActions(EscalationCreatedEvent evt)
    {
        var actions = new List<NotificationAction>
        {
            new() { Label = "View Escalation", Action = "navigate", Target = $"/escalations/{evt.EscalationId}", Variant = "primary" }
        };

        if (evt.SagaId != Guid.Empty)
        {
            actions.Add(new() { Label = "View Workflow", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "secondary" });
        }

        // Add suggested actions if available
        foreach (var suggestedAction in evt.SuggestedActions.Take(2))
        {
            actions.Add(new() { Label = suggestedAction, Action = "info", Variant = "outline" });
        }

        return actions;
    }

    private Guid GetOpsTeamUserId()
    {
        var opsUserId = _configuration["Notifications:OpsTeamUserId"];
        if (!string.IsNullOrEmpty(opsUserId) && Guid.TryParse(opsUserId, out var userId))
        {
            return userId;
        }

        _logger.LogWarning("No OpsTeamUserId configured, using default");
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static Guid? ParseGuidOrDefault(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays} day(s)";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} hour(s)";
        return $"{(int)duration.TotalMinutes} minute(s)";
    }
}
