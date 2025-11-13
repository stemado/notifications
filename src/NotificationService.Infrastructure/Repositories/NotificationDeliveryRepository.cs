using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for notification delivery tracking
/// </summary>
public class NotificationDeliveryRepository : INotificationDeliveryRepository
{
    private readonly NotificationDbContext _context;

    public NotificationDeliveryRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDelivery> CreateAsync(NotificationDelivery delivery)
    {
        _context.NotificationDeliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return delivery;
    }

    public async Task<List<NotificationDelivery>> GetByNotificationIdAsync(Guid notificationId)
    {
        return await _context.NotificationDeliveries
            .Where(d => d.NotificationId == notificationId)
            .ToListAsync();
    }

    public async Task UpdateAsync(NotificationDelivery delivery)
    {
        _context.NotificationDeliveries.Update(delivery);
        await _context.SaveChangesAsync();
    }
}
