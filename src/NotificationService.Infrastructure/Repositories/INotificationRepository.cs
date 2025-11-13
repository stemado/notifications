using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository interface for notification data access
/// </summary>
public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<List<Notification>> GetActiveForUserAsync(Guid userId);
    Task<List<Notification>> GetByTenantAsync(Guid tenantId);
    Task<Notification?> GetByGroupKeyAsync(string groupKey);
    Task<Notification?> GetActiveForSagaAsync(Guid sagaId);
    Task<List<Notification>> GetNotificationsDueForRepeatAsync();
    Task UpdateAsync(Notification notification);
    Task DeleteAsync(Guid id);
    Task<int> DeleteAcknowledgedOlderThanAsync(DateTime cutoffDate);
    Task<int> ExpireNotificationsAsync(DateTime now);
}
