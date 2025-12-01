using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles ImportCompletedEvent by creating notifications and dispatching them via multi-channel dispatcher.
/// </summary>
public class ImportCompletedNotificationHandler : IEventHandler<ImportCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ImportCompletedNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public ImportCompletedNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<ImportCompletedNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(ImportCompletedEvent evt)
    {
        try
        {
            var severity = DetermineSeverity(evt);

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,

                Severity = severity,
                Title = $"Import Completed: {evt.ClientName}",
                Message = BuildMessage(evt),

                SagaId = evt.SagaId,
                ClientId = ParseGuidOrDefault(evt.ClientId),
                EventType = nameof(ImportCompletedEvent),

                ExpiresAt = DateTime.UtcNow.AddDays(7),
                GroupKey = $"import:completed:{evt.SagaId}",

                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Details", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created and dispatched notification {NotificationId} for import completed {SagaId} with severity {Severity}",
                notification.Id, evt.SagaId, severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ImportCompletedEvent for saga {SagaId}", evt.SagaId);
            throw;
        }
    }

    private NotificationSeverity DetermineSeverity(ImportCompletedEvent evt)
    {
        // If there are failures, escalate severity
        if (evt.FailureCount > 0)
        {
            var failureRate = evt.TotalRecords > 0
                ? (double)evt.FailureCount / evt.TotalRecords
                : 0;

            if (failureRate > 0.1) return NotificationSeverity.Warning;
        }

        return NotificationSeverity.Info;
    }

    private string BuildMessage(ImportCompletedEvent evt)
    {
        var parts = new List<string>
        {
            $"File '{evt.FileName}' processed successfully.",
            $"{evt.SuccessCount}/{evt.TotalRecords} records imported."
        };

        if (evt.NewHireCount > 0 || evt.TerminationCount > 0 || evt.DemographicChangeCount > 0)
        {
            parts.Add($"Changes: {evt.NewHireCount} new hires, {evt.TerminationCount} terminations, {evt.DemographicChangeCount} demographics.");
        }

        if (evt.FailureCount > 0)
        {
            parts.Add($"Failures: {evt.FailureCount} records.");
        }

        parts.Add($"Duration: {FormatDuration(evt.Duration)}");

        return string.Join(" ", parts);
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
