using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Domain.Models.Preferences;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Service implementation for managing notification subscriptions
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;

    public SubscriptionService(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<NotificationSubscription>> GetUserSubscriptionsAsync(Guid userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    public async Task<NotificationSubscription> SubscribeAsync(
        Guid userId,
        Guid? clientId,
        Guid? sagaId,
        NotificationSeverity minSeverity)
    {
        var existing = await _repository.GetByCompositeKeyAsync(userId, clientId, sagaId);

        if (existing != null)
        {
            existing.MinSeverity = minSeverity;
            await _repository.UpdateAsync(existing);
            return existing;
        }

        var subscription = new NotificationSubscription
        {
            UserId = userId,
            ClientId = clientId,
            SagaId = sagaId,
            MinSeverity = minSeverity
        };

        return await _repository.CreateAsync(subscription);
    }

    public async Task UnsubscribeAsync(Guid userId, Guid? clientId, Guid? sagaId)
    {
        await _repository.DeleteAsync(userId, clientId, sagaId);
    }

    public async Task<List<Guid>> GetSubscribedUsersAsync(Guid? clientId, Guid? sagaId, NotificationSeverity severity)
    {
        List<NotificationSubscription> subscriptions;

        if (sagaId.HasValue)
        {
            // Get users subscribed to this specific saga
            subscriptions = await _repository.GetBySagaIdAsync(sagaId.Value);
        }
        else if (clientId.HasValue)
        {
            // Get users subscribed to this client
            subscriptions = await _repository.GetByClientIdAsync(clientId.Value);
        }
        else
        {
            return new List<Guid>();
        }

        // Filter by severity and return user IDs
        return subscriptions
            .Where(s => severity >= s.MinSeverity)
            .Select(s => s.UserId)
            .Distinct()
            .ToList();
    }

    public async Task<bool> ShouldReceiveNotificationAsync(Guid userId, Notification notification)
    {
        var subscriptions = await _repository.GetByUserIdAsync(userId);

        if (subscriptions.Count == 0)
        {
            // No subscriptions = receive all notifications (default behavior)
            return true;
        }

        // Check if any subscription matches this notification
        foreach (var subscription in subscriptions)
        {
            // Check severity first
            if (notification.Severity < subscription.MinSeverity)
            {
                continue;
            }

            // Check saga-specific subscription
            if (subscription.SagaId.HasValue)
            {
                if (subscription.SagaId == notification.SagaId)
                {
                    return true;
                }
                continue;
            }

            // Check client-specific subscription
            if (subscription.ClientId.HasValue)
            {
                if (subscription.ClientId == notification.ClientId)
                {
                    return true;
                }
                continue;
            }

            // Wildcard subscription (null client and saga)
            if (!subscription.ClientId.HasValue && !subscription.SagaId.HasValue)
            {
                return true;
            }
        }

        return false;
    }
}
