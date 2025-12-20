using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Hubs;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// SignalR notification channel (Phase 2)
/// </summary>
public class SignalRChannel : INotificationChannel
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly ILogger<SignalRChannel> _logger;

    public SignalRChannel(
        IHubContext<NotificationHub> hubContext,
        INotificationDeliveryRepository deliveryRepository,
        ILogger<SignalRChannel> logger)
    {
        _hubContext = hubContext;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
    }

    public string ChannelName => "SignalR";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            Channel = NotificationChannel.SignalR,
            Status = DeliveryStatus.Processing,
            AttemptCount = 1
        };

        try
        {
            // Send to user-specific group
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("NewNotification", notification);

            // If for ops team (TenantId is null), also send to ops-team group
            if (notification.TenantId == null)
            {
                await _hubContext.Clients
                    .Group("ops-team")
                    .SendAsync("NewNotification", notification);
            }
            // If for a specific tenant, send to tenant group
            else
            {
                await _hubContext.Clients
                    .Group($"tenant:{notification.TenantId}")
                    .SendAsync("NewNotification", notification);
            }

            delivery.Status = DeliveryStatus.Delivered;
            delivery.DeliveredAt = DateTime.UtcNow;
            _logger.LogInformation(
                "SignalR notification {NotificationId} delivered to user {UserId}",
                notification.Id, userId);
        }
        catch (Exception ex)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.FailedAt = DateTime.UtcNow;
            delivery.ErrorMessage = ex.Message;
            _logger.LogError(ex,
                "Error delivering SignalR notification {NotificationId} to user {UserId}",
                notification.Id, userId);
        }
        finally
        {
            await _deliveryRepository.CreateAsync(delivery);
        }
    }
}
