using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for notification data access
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<Notification>> GetActiveForUserAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.AcknowledgedAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetByTenantAsync(Guid tenantId)
    {
        return await _context.Notifications
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification?> GetByGroupKeyAsync(string groupKey)
    {
        return await _context.Notifications
            .Where(n => n.GroupKey == groupKey && n.AcknowledgedAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Notification?> GetActiveForSagaAsync(Guid sagaId)
    {
        return await _context.Notifications
            .Where(n => n.SagaId == sagaId && n.AcknowledgedAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Notification>> GetNotificationsDueForRepeatAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Notifications
            .Where(n => n.RepeatInterval != null
                     && n.AcknowledgedAt == null
                     && (n.LastRepeatedAt == null ||
                         n.LastRepeatedAt.Value.AddMinutes(n.RepeatInterval.Value) <= now))
            .ToListAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var notification = await GetByIdAsync(id);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> DeleteAcknowledgedOlderThanAsync(DateTime cutoffDate)
    {
        var toDelete = await _context.Notifications
            .Where(n => n.AcknowledgedAt != null && n.AcknowledgedAt < cutoffDate)
            .ToListAsync();

        _context.Notifications.RemoveRange(toDelete);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> ExpireNotificationsAsync(DateTime now)
    {
        var toExpire = await _context.Notifications
            .Where(n => n.ExpiresAt != null && n.ExpiresAt < now && n.AcknowledgedAt == null)
            .ToListAsync();

        foreach (var notification in toExpire)
        {
            notification.DismissedAt = now;
        }

        return await _context.SaveChangesAsync();
    }
}
