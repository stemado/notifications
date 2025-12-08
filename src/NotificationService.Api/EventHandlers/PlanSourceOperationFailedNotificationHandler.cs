using NotificationService.Api.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles PlanSourceOperationFailedEvent by creating notifications and dispatching them via multi-channel dispatcher
/// </summary>
public class PlanSourceOperationFailedNotificationHandler : IEventHandler<PlanSourceOperationFailedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<PlanSourceOperationFailedNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public PlanSourceOperationFailedNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<PlanSourceOperationFailedNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(PlanSourceOperationFailedEvent evt)
    {
        try
        {
            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,
                Severity = evt.Severity,
                Title = $"PlanSource {evt.OperationType} Failed",
                Message = $"{evt.ClientName}: {evt.OperationType} failed. " +
                         $"Error: {evt.ErrorMessage}. " +
                         $"Attempt {evt.AttemptNumber}/{evt.MaxRetries}. " +
                         (evt.IsRetryable ? "Will retry." : "Non-retryable."),
                SagaId = evt.SagaId,
                ClientId = evt.ClientId,
                EventId = evt.Id,
                EventType = nameof(PlanSourceOperationFailedEvent),
                RequiresAck = !evt.IsRetryable,
                GroupKey = $"plansource:failed:{evt.SagaId}:{evt.OperationType}",
                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Workflow", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" },
                    new() { Label = "Force Retry", Action = "api-call", Target = $"/api/workflows/{evt.SagaId}/retry", Variant = "secondary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created and dispatched PlanSource failure notification {NotificationId} for saga {SagaId}",
                notification.Id, evt.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PlanSourceOperationFailedEvent for saga {SagaId}", evt.SagaId);
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
