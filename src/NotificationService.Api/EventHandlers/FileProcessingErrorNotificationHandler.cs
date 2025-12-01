using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles FileProcessingErrorEvent by creating notifications and dispatching them via multi-channel dispatcher.
/// Groups similar errors to prevent notification spam.
/// </summary>
public class FileProcessingErrorNotificationHandler : IEventHandler<FileProcessingErrorEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<FileProcessingErrorNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public FileProcessingErrorNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        ILogger<FileProcessingErrorNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(FileProcessingErrorEvent evt)
    {
        try
        {
            // Map client severity to domain severity
            var severity = MapSeverity(evt.Severity);

            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,

                Severity = severity,
                Title = $"File Error: {evt.ErrorType} - {evt.ClientName}",
                Message = BuildMessage(evt),

                SagaId = evt.SagaId,
                ClientId = ParseGuidOrDefault(evt.ClientId),
                EventType = nameof(FileProcessingErrorEvent),

                RequiresAck = !evt.IsRecoverable,
                ExpiresAt = evt.IsRecoverable ? DateTime.UtcNow.AddHours(4) : null,
                // Group by client, error type, and hour to prevent spam
                GroupKey = $"file:error:{evt.ClientId}:{evt.ErrorType}:{DateTime.UtcNow:yyyyMMddHH}",

                Actions = BuildActions(evt)
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogWarning(
                "Created and dispatched notification {NotificationId} for file processing error. Client: {ClientId}, ErrorType: {ErrorType}, Recoverable: {IsRecoverable}",
                notification.Id, evt.ClientId, evt.ErrorType, evt.IsRecoverable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FileProcessingErrorEvent for client {ClientId}", evt.ClientId);
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

    private string BuildMessage(FileProcessingErrorEvent evt)
    {
        var message = $"Error processing file '{evt.FilePath}': {evt.ErrorMessage}";

        if (!string.IsNullOrEmpty(evt.Resolution))
        {
            message += $" Suggested resolution: {evt.Resolution}";
        }

        if (evt.IsRecoverable)
        {
            message += " (This error may resolve automatically)";
        }

        return message;
    }

    private List<NotificationAction> BuildActions(FileProcessingErrorEvent evt)
    {
        var actions = new List<NotificationAction>();

        if (evt.SagaId.HasValue)
        {
            actions.Add(new() { Label = "View Workflow", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" });
        }

        if (!string.IsNullOrEmpty(evt.ClientId))
        {
            actions.Add(new() { Label = "View Client", Action = "navigate", Target = $"/clients/{evt.ClientId}", Variant = "secondary" });
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
}
