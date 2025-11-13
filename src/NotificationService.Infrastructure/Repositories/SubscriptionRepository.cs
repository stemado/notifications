using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Models.Preferences;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for notification subscriptions
/// </summary>
public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly NotificationDbContext _context;

    public SubscriptionRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationSubscription>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Set<NotificationSubscription>()
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<NotificationSubscription>> GetByClientIdAsync(Guid clientId)
    {
        return await _context.Set<NotificationSubscription>()
            .Where(s => s.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<List<NotificationSubscription>> GetBySagaIdAsync(Guid sagaId)
    {
        return await _context.Set<NotificationSubscription>()
            .Where(s => s.SagaId == sagaId)
            .ToListAsync();
    }

    public async Task<NotificationSubscription?> GetByCompositeKeyAsync(Guid userId, Guid? clientId, Guid? sagaId)
    {
        return await _context.Set<NotificationSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId
                                   && s.ClientId == clientId
                                   && s.SagaId == sagaId);
    }

    public async Task<NotificationSubscription> CreateAsync(NotificationSubscription subscription)
    {
        _context.Set<NotificationSubscription>().Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task UpdateAsync(NotificationSubscription subscription)
    {
        _context.Set<NotificationSubscription>().Update(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid userId, Guid? clientId, Guid? sagaId)
    {
        var subscription = await GetByCompositeKeyAsync(userId, clientId, sagaId);
        if (subscription != null)
        {
            _context.Set<NotificationSubscription>().Remove(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
