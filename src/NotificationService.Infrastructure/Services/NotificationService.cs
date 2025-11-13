using NotificationService.Domain.DTOs;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Core notification service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Notification> CreateAsync(CreateNotificationRequest request)
    {
        var notification = MapToNotification(request);
        return await _repository.CreateAsync(notification);
    }

    public async Task<Notification> CreateOrUpdateAsync(CreateNotificationRequest request)
    {
        // If GroupKey is provided, check for existing notification
        if (!string.IsNullOrEmpty(request.GroupKey))
        {
            var existing = await _repository.GetByGroupKeyAsync(request.GroupKey);
            if (existing != null)
            {
                // Update existing notification
                existing.GroupCount++;
                existing.Severity = request.Severity; // Update severity (may have escalated)
                existing.Message = request.Message; // Update message
                existing.LastRepeatedAt = null; // Reset repeat timer
                existing.ExpiresAt = request.ExpiresAt;
                existing.RepeatInterval = request.RepeatInterval;
                existing.Metadata = request.Metadata;

                await _repository.UpdateAsync(existing);
                return existing;
            }
        }

        // Create new notification
        return await CreateAsync(request);
    }

    public async Task<List<Notification>> GetActiveForUserAsync(Guid userId)
    {
        return await _repository.GetActiveForUserAsync(userId);
    }

    public async Task<List<Notification>> GetByTenantAsync(Guid tenantId)
    {
        return await _repository.GetByTenantAsync(tenantId);
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Notification?> GetActiveForSagaAsync(Guid sagaId)
    {
        return await _repository.GetActiveForSagaAsync(sagaId);
    }

    public async Task<List<Notification>> GetNotificationsDueForRepeatAsync()
    {
        return await _repository.GetNotificationsDueForRepeatAsync();
    }

    public async Task AcknowledgeAsync(Guid notificationId, Guid userId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
        {
            throw new InvalidOperationException("Notification not found or user not authorized");
        }

        notification.AcknowledgedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(notification);
    }

    public async Task DismissAsync(Guid notificationId, Guid userId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
        {
            throw new InvalidOperationException("Notification not found or user not authorized");
        }

        notification.DismissedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(notification);
    }

    public async Task SnoozeAsync(Guid notificationId, int minutes)
    {
        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification == null)
        {
            throw new InvalidOperationException("Notification not found");
        }

        // Temporarily dismiss and set to repeat after snooze period
        notification.DismissedAt = DateTime.UtcNow;
        notification.LastRepeatedAt = DateTime.UtcNow;

        // Override repeat interval with snooze duration
        var originalInterval = notification.RepeatInterval;
        notification.RepeatInterval = minutes;
        notification.Metadata["SnoozeOriginalInterval"] = originalInterval ?? 0;
        notification.Metadata["SnoozedUntil"] = DateTime.UtcNow.AddMinutes(minutes);

        await _repository.UpdateAsync(notification);
    }

    public async Task UpdateLastRepeatedAsync(Guid notificationId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification == null)
        {
            throw new InvalidOperationException("Notification not found");
        }

        notification.LastRepeatedAt = DateTime.UtcNow;

        // If this was snoozed, restore original repeat interval
        if (notification.Metadata.ContainsKey("SnoozeOriginalInterval"))
        {
            notification.RepeatInterval = (int)notification.Metadata["SnoozeOriginalInterval"];
            notification.Metadata.Remove("SnoozeOriginalInterval");
            notification.Metadata.Remove("SnoozedUntil");
        }

        await _repository.UpdateAsync(notification);
    }

    public async Task ExpireOldNotificationsAsync()
    {
        await _repository.ExpireNotificationsAsync(DateTime.UtcNow);
    }

    public async Task DeleteAcknowledgedAsync(int daysOld)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        await _repository.DeleteAcknowledgedOlderThanAsync(cutoffDate);
    }

    private Notification MapToNotification(CreateNotificationRequest request)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TenantId = request.TenantId,
            Severity = request.Severity,
            Title = request.Title,
            Message = request.Message,
            SagaId = request.SagaId,
            ClientId = request.ClientId,
            EventId = request.EventId,
            EventType = request.EventType,
            CreatedAt = DateTime.UtcNow,
            RepeatInterval = request.RepeatInterval,
            RequiresAck = request.RequiresAck,
            ExpiresAt = request.ExpiresAt,
            GroupKey = request.GroupKey,
            GroupCount = 1,
            Actions = request.Actions,
            Metadata = request.Metadata
        };
    }
}
