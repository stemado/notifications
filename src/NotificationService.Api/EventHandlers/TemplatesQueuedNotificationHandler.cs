using Core.ImportHistoryScheduler.Services;
using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles TemplatesQueuedEvent by:
/// 1. Scheduling an import history check via IImportHistoryScheduler
/// 2. Creating a notification to track the scheduled check
///
/// This handler is the EVENT-DRIVEN fix for the broken scheduler chain.
/// Previously, ScheduleCheckAsync was only called directly in client code,
/// which meant if the direct call failed, no check would be scheduled.
/// </summary>
public class TemplatesQueuedNotificationHandler : IEventHandler<TemplatesQueuedEvent>
{
    private readonly IImportHistoryScheduler _importHistoryScheduler;
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<TemplatesQueuedNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public TemplatesQueuedNotificationHandler(
        IImportHistoryScheduler importHistoryScheduler,
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<TemplatesQueuedNotificationHandler> logger,
        IConfiguration configuration)
    {
        _importHistoryScheduler = importHistoryScheduler;
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(TemplatesQueuedEvent evt)
    {
        if (string.IsNullOrEmpty(evt.ClientId))
        {
            _logger.LogWarning("TemplatesQueuedEvent received with empty ClientId, skipping scheduler");
            return;
        }

        try
        {
            // CRITICAL: Schedule the import history check
            // This is the key fix - ensuring the scheduler is triggered via event
            var delayMinutes = evt.DelayMinutes > 0 ? evt.DelayMinutes : 90;

            var checkId = await _importHistoryScheduler.ScheduleCheckAsync(
                evt.ClientId,
                delayMinutes);

            _logger.LogInformation(
                "Scheduled import history check {CheckId} for client {ClientId} after {DelayMinutes} minutes. " +
                "SagaId: {SagaId}, TemplateCount: {TemplateCount}, ImportTypes: [{ImportTypes}]",
                checkId,
                evt.ClientId,
                delayMinutes,
                evt.SagaId,
                evt.TemplateCount,
                string.Join(", ", evt.ImportTypes));

            // Create a notification to track the scheduled check
            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,
                Severity = NotificationSeverity.Info,
                Title = $"Templates Queued: {evt.ClientName}",
                Message = BuildMessage(evt, checkId, delayMinutes),
                SagaId = evt.SagaId,
                ClientId = ParseGuidOrDefault(evt.ClientId),
                EventType = nameof(TemplatesQueuedEvent),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                GroupKey = $"templates:queued:{evt.SagaId}",
                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Timeline", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created notification {NotificationId} for templates queued event. " +
                "Import history check scheduled for {ScheduledTime}",
                notification.Id,
                DateTime.UtcNow.AddMinutes(delayMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling TemplatesQueuedEvent for client {ClientId}, saga {SagaId}. " +
                "Import history check may not be scheduled!",
                evt.ClientId, evt.SagaId);
            throw;
        }
    }

    private string BuildMessage(TemplatesQueuedEvent evt, Guid checkId, int delayMinutes)
    {
        var parts = new List<string>
        {
            $"{evt.TemplateCount} template file(s) queued for import."
        };

        if (evt.ImportTypes.Any())
        {
            parts.Add($"Types: {string.Join(", ", evt.ImportTypes)}.");
        }

        if (evt.QueueIds.Any())
        {
            parts.Add($"Queue IDs: {string.Join(", ", evt.QueueIds)}.");
        }

        parts.Add($"Import history check scheduled in {delayMinutes} minutes.");

        return string.Join(" ", parts);
    }

    private Guid GetOpsTeamUserId()
    {
        var opsUserId = _configuration["Notifications:OpsTeamUserId"];
        if (!string.IsNullOrEmpty(opsUserId) && Guid.TryParse(opsUserId, out var userId))
        {
            return userId;
        }

        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static Guid? ParseGuidOrDefault(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
