using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository for notification delivery tracking
/// </summary>
public interface INotificationDeliveryRepository
{
    Task<NotificationDelivery> CreateAsync(NotificationDelivery delivery);
    Task<List<NotificationDelivery>> GetByNotificationIdAsync(Guid notificationId);
    Task UpdateAsync(NotificationDelivery delivery);
}
