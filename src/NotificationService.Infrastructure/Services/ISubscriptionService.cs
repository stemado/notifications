using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Domain.Models.Preferences;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Service for managing notification subscriptions (Phase 2)
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets all subscriptions for a user
    /// </summary>
    Task<List<NotificationSubscription>> GetUserSubscriptionsAsync(Guid userId);

    /// <summary>
    /// Subscribes a user to notifications for a client/saga
    /// </summary>
    Task<NotificationSubscription> SubscribeAsync(Guid userId, Guid? clientId, Guid? sagaId, NotificationSeverity minSeverity);

    /// <summary>
    /// Unsubscribes a user from a client/saga
    /// </summary>
    Task UnsubscribeAsync(Guid userId, Guid? clientId, Guid? sagaId);

    /// <summary>
    /// Gets all users subscribed to notifications for a specific client/saga
    /// </summary>
    Task<List<Guid>> GetSubscribedUsersAsync(Guid? clientId, Guid? sagaId, NotificationSeverity severity);

    /// <summary>
    /// Checks if a user should receive a notification based on their subscriptions
    /// </summary>
    Task<bool> ShouldReceiveNotificationAsync(Guid userId, Notification notification);
}
