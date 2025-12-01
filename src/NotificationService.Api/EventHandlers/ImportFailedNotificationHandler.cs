using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles ImportFailedEvent by creating urgent notifications and dispatching them via multi-channel dispatcher.
/// </summary>
public class ImportFailedNotificationHandler : IEventHandler<ImportFailedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ImportFailedNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public ImportFailedNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<ImportFailedNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(ImportFailedEvent evt)
    {
        try
        {
            var severity = DetermineSeverity(evt);
            var repeatInterval = DetermineRepeatInterval(severity);

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,

                Severity = severity,
                Title = $"Import Failed: {evt.ClientName}",
                Message = BuildMessage(evt),

                SagaId = evt.SagaId,
                ClientId = ParseGuidOrDefault(evt.ClientId),
                EventType = nameof(ImportFailedEvent),

                RepeatInterval = repeatInterval,
                RequiresAck = true,
                GroupKey = $"import:failed:{evt.SagaId}",

                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Error", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" },
                    new() { Label = "Retry Import", Action = "api", Target = $"/api/workflows/{evt.SagaId}/retry", Variant = "secondary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogWarning(
                "Created and dispatched notification {NotificationId} for import failure {SagaId} with severity {Severity}",
                notification.Id, evt.SagaId, severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ImportFailedEvent for saga {SagaId}", evt.SagaId);
            throw;
        }
    }

    private NotificationSeverity DetermineSeverity(ImportFailedEvent evt)
    {
        // Escalated failures are critical
        if (evt.WasEscalated) return NotificationSeverity.Critical;

        // Multiple retries indicate a persistent issue
        if (evt.RetryCount >= 3) return NotificationSeverity.Critical;
        if (evt.RetryCount >= 1) return NotificationSeverity.Urgent;

        return NotificationSeverity.Warning;
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

    private string BuildMessage(ImportFailedEvent evt)
    {
        var message = $"File '{evt.FileName}' failed to import at state '{evt.FailedAtState}'.";

        if (!string.IsNullOrEmpty(evt.ErrorMessage))
        {
            message += $" Error: {evt.ErrorMessage}";
        }

        if (evt.RetryCount > 0)
        {
            message += $" (Retry attempt {evt.RetryCount})";
        }

        return message;
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
}
