using Hangfire;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.Jobs;

/// <summary>
/// Backup polling job that checks for stuck sagas without notifications
/// This is a safety net in case event handlers fail
/// Runs every 15 minutes
/// </summary>
public class NotificationBackupPollingJob
{
    // Note: ISagaRepository would be defined in your saga domain
    // This is a placeholder interface for the example
    private readonly ILogger<NotificationBackupPollingJob> _logger;
    private readonly INotificationService _notificationService;

    public NotificationBackupPollingJob(
        ILogger<NotificationBackupPollingJob> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task Execute()
    {
        try
        {
            _logger.LogInformation("Starting notification backup polling job");

            // TODO: Phase 1 - Implement when ISagaRepository is available
            // Find sagas stuck >24h with no active notification
            // var stuckSagas = await _sagaRepo.GetStuckSagasAsync(TimeSpan.FromHours(24));
            //
            // foreach (var saga in stuckSagas)
            // {
            //     var existing = await _notificationService.GetActiveForSagaAsync(saga.Id);
            //     if (existing == null)
            //     {
            //         // Create missed notification
            //         // This handles cases where event handlers failed
            //         _logger.LogWarning(
            //             "Found stuck saga {SagaId} without notification, creating one",
            //             saga.Id);
            //
            //         // Create notification using the event handler pattern
            //         var evt = new SagaStuckEvent
            //         {
            //             SagaId = saga.Id,
            //             ClientId = saga.ClientId,
            //             ClientName = saga.ClientName,
            //             StuckDuration = saga.GetStuckDuration()
            //         };
            //         await _eventHandler.Handle(evt);
            //     }
            // }

            _logger.LogInformation("Notification backup polling job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification backup polling job");
            throw;
        }
    }
}
