using NotificationService.Domain.Models.Preferences;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository for notification subscriptions
/// </summary>
public interface ISubscriptionRepository
{
    Task<List<NotificationSubscription>> GetByUserIdAsync(Guid userId);
    Task<List<NotificationSubscription>> GetByClientIdAsync(Guid clientId);
    Task<List<NotificationSubscription>> GetBySagaIdAsync(Guid sagaId);
    Task<NotificationSubscription?> GetByCompositeKeyAsync(Guid userId, Guid? clientId, Guid? sagaId);
    Task<NotificationSubscription> CreateAsync(NotificationSubscription subscription);
    Task UpdateAsync(NotificationSubscription subscription);
    Task DeleteAsync(Guid userId, Guid? clientId, Guid? sagaId);
}
