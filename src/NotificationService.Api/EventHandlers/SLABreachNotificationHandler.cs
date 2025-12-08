using NotificationService.Api.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles SLABreachEvent by creating notifications and dispatching them via multi-channel dispatcher
/// </summary>
public class SLABreachNotificationHandler : IEventHandler<SLABreachEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<SLABreachNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public SLABreachNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<SLABreachNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(SLABreachEvent evt)
    {
        try
        {
            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,
                Severity = evt.Severity,
                Title = $"SLA Breach: {evt.SLAType}",
                Message = $"{evt.ClientName} exceeded {evt.SLAType} SLA. Actual: {evt.ActualMinutes}min vs Threshold: {evt.ThresholdMinutes}min. State: {evt.CurrentState}",
                SagaId = evt.SagaId,
                ClientId = evt.ClientId,
                EventId = evt.Id,
                EventType = nameof(SLABreachEvent),
                RepeatInterval = evt.Severity >= NotificationSeverity.Critical ? 15 : null,
                RequiresAck = evt.Severity >= NotificationSeverity.Urgent,
                GroupKey = $"sla:breach:{evt.SagaId}:{evt.SLAType}",
                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Workflow", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created and dispatched SLA breach notification {NotificationId} for saga {SagaId} with severity {Severity}",
                notification.Id, evt.SagaId, evt.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SLABreachEvent for saga {SagaId}", evt.SagaId);
            throw;
        }
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
}
