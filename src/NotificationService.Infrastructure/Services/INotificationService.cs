using NotificationService.Domain.DTOs;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Core notification service interface
/// </summary>
public interface INotificationService
{
    // Create notifications
    /// <summary>
    /// Creates a new notification
    /// </summary>
    Task<Notification> CreateAsync(CreateNotificationRequest request);

    /// <summary>
    /// Creates a new notification or updates existing one using GroupKey for deduplication
    /// </summary>
    Task<Notification> CreateOrUpdateAsync(CreateNotificationRequest request);

    // Query notifications
    /// <summary>
    /// Gets all active (unacknowledged) notifications for a user
    /// </summary>
    Task<List<Notification>> GetActiveForUserAsync(Guid userId);

    /// <summary>
    /// Gets all notifications for a tenant (multi-tenant ready)
    /// </summary>
    Task<List<Notification>> GetByTenantAsync(Guid tenantId);

    /// <summary>
    /// Gets a notification by ID
    /// </summary>
    Task<Notification?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets active notification for a saga (Phase 1 - for backup polling job)
    /// </summary>
    Task<Notification?> GetActiveForSagaAsync(Guid sagaId);

    /// <summary>
    /// Gets notifications that are due for repeating
    /// </summary>
    Task<List<Notification>> GetNotificationsDueForRepeatAsync();

    // Update notifications
    /// <summary>
    /// Marks a notification as acknowledged
    /// </summary>
    Task AcknowledgeAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Dismisses a notification
    /// </summary>
    Task DismissAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Snoozes a notification for specified minutes
    /// </summary>
    Task SnoozeAsync(Guid notificationId, int minutes);

    /// <summary>
    /// Updates the last repeated timestamp for a notification
    /// </summary>
    Task UpdateLastRepeatedAsync(Guid notificationId);

    // Cleanup
    /// <summary>
    /// Expires old notifications based on ExpiresAt field
    /// </summary>
    Task ExpireOldNotificationsAsync();

    /// <summary>
    /// Deletes acknowledged notifications older than specified days
    /// </summary>
    Task DeleteAcknowledgedAsync(int daysOld);
}
