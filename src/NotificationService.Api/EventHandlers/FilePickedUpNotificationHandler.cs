using System.Text.Json;
using NotificationService.Api.Events;
using NotificationService.Client.Events;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Services;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Services;

namespace NotificationService.Api.EventHandlers;

/// <summary>
/// Handles FilePickedUpEvent by creating notifications and dispatching them via multi-channel dispatcher.
/// Also publishes to outbound routing for email/SMS/Teams delivery.
/// </summary>
public class FilePickedUpNotificationHandler : IEventHandler<FilePickedUpEvent>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IOutboundRouter _outboundRouter;
    private readonly ILogger<FilePickedUpNotificationHandler> _logger;
    private readonly IConfiguration _configuration;

    public FilePickedUpNotificationHandler(
        INotificationService notificationService,
        INotificationDispatcher dispatcher,
        IOutboundRouter outboundRouter,
        ILogger<FilePickedUpNotificationHandler> logger,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        _outboundRouter = outboundRouter;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Handle(FilePickedUpEvent evt)
    {
        try
        {
            var notification = await _notificationService.CreateOrUpdateAsync(new CreateNotificationRequest
            {
                UserId = GetOpsTeamUserId(),
                TenantId = evt.TenantId,

                Severity = NotificationSeverity.Info,
                Title = $"File Picked Up: {evt.ClientName}",
                Message = BuildMessage(evt),

                SagaId = evt.SagaId,
                ClientId = ParseGuidOrDefault(evt.ClientId),
                EventType = nameof(FilePickedUpEvent),

                ExpiresAt = DateTime.UtcNow.AddHours(4), // Short-lived - will be superseded by completion/failure
                GroupKey = $"file:pickedup:{evt.SagaId}",

                Actions = new List<NotificationAction>
                {
                    new() { Label = "View Workflow", Action = "navigate", Target = $"/timeline/{evt.SagaId}", Variant = "primary" }
                }
            });

            await _dispatcher.DispatchAsync(notification);

            _logger.LogInformation(
                "Created and dispatched in-app notification {NotificationId} for file picked up. SagaId={SagaId}, Client={ClientId}, File={FileName}",
                notification.Id, evt.SagaId, evt.ClientId, evt.FileName);

            // Publish to outbound routing for email/SMS/Teams delivery based on policies
            await PublishToOutboundRoutingAsync(evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FilePickedUpEvent for saga {SagaId}", evt.SagaId);
            throw;
        }
    }

    private async Task PublishToOutboundRoutingAsync(FilePickedUpEvent evt)
    {
        try
        {
            var outboundEvent = new OutboundEvent
            {
                Id = Guid.NewGuid(),
                Service = SourceService.CensusOrchestration,
                Topic = NotificationTopic.FileProcessingStarted,
                ClientId = evt.ClientId,
                Severity = NotificationSeverity.Info,
                Subject = $"File Picked Up: {evt.ClientName}",
                Body = BuildMessage(evt),
                SagaId = evt.SagaId,
                CorrelationId = Guid.TryParse(evt.CorrelationId, out var correlationId) ? correlationId : null,
                CreatedAt = DateTime.UtcNow,
                Payload = new Dictionary<string, JsonElement>
                {
                    ["clientName"] = JsonSerializer.SerializeToElement(evt.ClientName),
                    ["fileName"] = JsonSerializer.SerializeToElement(evt.FileName),
                    ["filePath"] = JsonSerializer.SerializeToElement(evt.FilePath),
                    ["fileSizeBytes"] = JsonSerializer.SerializeToElement(evt.FileSizeBytes ?? 0),
                    ["pickedUpAt"] = JsonSerializer.SerializeToElement(evt.PickedUpAt.ToString("O"))
                }
            };

            var eventId = await _outboundRouter.PublishAsync(outboundEvent);

            _logger.LogInformation(
                "Published outbound event {EventId} for file picked up. SagaId={SagaId}, Client={ClientId}",
                eventId, evt.SagaId, evt.ClientId);
        }
        catch (Exception ex)
        {
            // Don't fail the whole handler if outbound routing fails
            _logger.LogWarning(ex, "Failed to publish to outbound routing for SagaId {SagaId}", evt.SagaId);
        }
    }

    private string BuildMessage(FilePickedUpEvent evt)
    {
        var message = $"File '{evt.FileName}' detected and registered for processing.";

        if (evt.FileSizeBytes.HasValue && evt.FileSizeBytes > 0)
        {
            message += $" Size: {FormatFileSize(evt.FileSizeBytes.Value)}";
        }

        return message;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1_048_576)
            return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} bytes";
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
