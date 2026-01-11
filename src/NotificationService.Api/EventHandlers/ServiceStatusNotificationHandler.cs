using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles ServiceStatusEvent by creating notifications and dispatching them via multi-channel dispatcher
/// </summary>
public class ServiceStatusNotificationHandler : IEventHandler<ServiceStatusEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ServiceStatusNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public ServiceStatusNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<ServiceStatusNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(ServiceStatusEvent evt)
    {
        try
        {
            var severity = DetermineSeverity(evt);
            var repeatInterval = DetermineRepeatInterval(evt);
            var (title, message) = BuildContent(evt);

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = null,
                Severity = severity,
                Title = title,
                Message = message,
                EventId = evt.Id,
                EventType = nameof(ServiceStatusEvent),
                GroupKey = BuildGroupKey(evt),
                RepeatInterval = repeatInterval,
                RequiresAck = severity >= NotificationSeverity.Urgent,
                ExpiresAt = evt.EventType == ServiceStatusEventType.ServiceRecovered
                    ? DateTime.UtcNow.AddHours(4)
                    : null,
                Actions = BuildActions(evt)
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created and dispatched notification {NotificationId} for service status event. Service={ServiceId}, EventType={EventType}, Severity={Severity}",
                notification.Id, evt.ServiceId, evt.EventType, severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ServiceStatusEvent for service {ServiceId}", evt.ServiceId);
            throw;
        }
    }

    private static NotificationSeverity DetermineSeverity(ServiceStatusEvent evt)
    {
        return evt.EventType switch
        {
            ServiceStatusEventType.ServiceDown => NotificationSeverity.Critical,
            ServiceStatusEventType.IncidentCreated => NotificationSeverity.Urgent,
            ServiceStatusEventType.ServiceDegraded => NotificationSeverity.Warning,
            ServiceStatusEventType.ServiceRecovered => NotificationSeverity.Info,
            ServiceStatusEventType.IncidentResolved => NotificationSeverity.Info,
            _ => NotificationSeverity.Info
        };
    }

    private static int? DetermineRepeatInterval(ServiceStatusEvent evt)
    {
        return evt.EventType switch
        {
            ServiceStatusEventType.ServiceDown => 15,      // Every 15 minutes
            ServiceStatusEventType.IncidentCreated => 30,  // Every 30 minutes
            _ => null                                       // No repeat
        };
    }

    private static (string title, string message) BuildContent(ServiceStatusEvent evt)
    {
        return evt.EventType switch
        {
            ServiceStatusEventType.ServiceDown => (
                $"Service Down: {evt.ServiceName}",
                $"Service '{evt.ServiceName}' ({evt.Category}) is not responding. " +
                (evt.ErrorMessage != null ? $"Error: {evt.ErrorMessage}" : "Health check failed.")
            ),
            ServiceStatusEventType.ServiceDegraded => (
                $"Service Degraded: {evt.ServiceName}",
                $"Service '{evt.ServiceName}' is experiencing high latency. " +
                $"Response time: {evt.ResponseTimeMs}ms (threshold: {evt.ThresholdMs}ms)"
            ),
            ServiceStatusEventType.ServiceRecovered => (
                $"Service Recovered: {evt.ServiceName}",
                $"Service '{evt.ServiceName}' is now healthy. Previous status: {evt.PreviousStatus}"
            ),
            ServiceStatusEventType.IncidentCreated => (
                $"Incident Created: {evt.ServiceName}",
                $"New incident detected for service '{evt.ServiceName}'. " +
                (evt.ErrorMessage != null ? $"Error: {evt.ErrorMessage}" : "Service is experiencing issues.")
            ),
            ServiceStatusEventType.IncidentResolved => (
                $"Incident Resolved: {evt.ServiceName}",
                $"Incident for service '{evt.ServiceName}' has been resolved. " +
                $"Duration: {FormatDuration(evt.IncidentDuration ?? TimeSpan.Zero)}. " +
                $"Failed checks: {evt.FailedCheckCount ?? 0}"
            ),
            _ => ($"Service Status: {evt.ServiceName}", $"Status changed to {evt.CurrentStatus}")
        };
    }

    private static string BuildGroupKey(ServiceStatusEvent evt)
    {
        return evt.EventType switch
        {
            ServiceStatusEventType.IncidentCreated or ServiceStatusEventType.IncidentResolved
                => $"service:incident:{evt.IncidentId}",
            _ => $"service:status:{evt.ServiceId}:{evt.EventType}"
        };
    }

    private static List<NotificationAction> BuildActions(ServiceStatusEvent evt)
    {
        var actions = new List<NotificationAction>
        {
            new() { Label = "View Dashboard", Action = "navigate", Target = "/orchestration/service-status", Variant = "primary" }
        };

        if (evt.EventType is ServiceStatusEventType.ServiceDown or ServiceStatusEventType.ServiceDegraded)
        {
            actions.Add(new()
            {
                Label = "Trigger Health Check",
                Action = "navigate",
                Target = $"/api/service-status/{evt.ServiceId}/check",
                Variant = "secondary"
            });
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

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays} day(s)";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} hour(s)";
        return $"{(int)duration.TotalMinutes} minute(s)";
    }
}
