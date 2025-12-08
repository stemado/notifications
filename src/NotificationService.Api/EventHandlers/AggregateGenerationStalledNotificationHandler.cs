using NotificationService.Api.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles AggregateGenerationStalledEvent by creating notifications and dispatching them via multi-channel dispatcher
/// </summary>
public class AggregateGenerationStalledNotificationHandler : IEventHandler<AggregateGenerationStalledEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<AggregateGenerationStalledNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public AggregateGenerationStalledNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<AggregateGenerationStalledNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(AggregateGenerationStalledEvent evt)
    {
        try
        {
            var isMaxedOut = evt.WaitCount >= evt.MaxWaitCount;
            var severity = isMaxedOut ? NotificationSeverity.Urgent : evt.Severity;

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,
                Severity = severity,
                Title = "Aggregate Generation Stalled",
                Message = $"{evt.ClientName}: Aggregate stalled. " +
                         $"Checked {evt.WaitCount}/{evt.MaxWaitCount} times over {evt.MinutesWaiting} minutes. " +
                         (evt.FileName != null ? $"File: {evt.FileName}" : ""),
                SagaId = evt.SagaId,
                ClientId = evt.ClientId,
                EventId = evt.Id,
                EventType = nameof(AggregateGenerationStalledEvent),
                RequiresAck = isMaxedOut,
                GroupKey = $"aggregate:stalled:{evt.SagaId}",
                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Workflow", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" },
                    new() { Label = "Force Retry", Action = "api-call", Target = $"/api/workflows/{evt.SagaId}/retry", Variant = "secondary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created and dispatched aggregate stalled notification {NotificationId} for saga {SagaId}",
                notification.Id, evt.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AggregateGenerationStalledEvent for saga {SagaId}", evt.SagaId);
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
