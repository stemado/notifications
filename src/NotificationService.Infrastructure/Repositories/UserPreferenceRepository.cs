using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models.Preferences;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user notification preferences
/// </summary>
public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly NotificationDbContext _context;

    public UserPreferenceRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserNotificationPreference>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Set<UserNotificationPreference>()
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserNotificationPreference?> GetByUserAndChannelAsync(Guid userId, NotificationChannel channel)
    {
        return await _context.Set<UserNotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Channel == channel);
    }

    public async Task<UserNotificationPreference> CreateAsync(UserNotificationPreference preference)
    {
        _context.Set<UserNotificationPreference>().Add(preference);
        await _context.SaveChangesAsync();
        return preference;
    }

    public async Task UpdateAsync(UserNotificationPreference preference)
    {
        _context.Set<UserNotificationPreference>().Update(preference);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid userId, NotificationChannel channel)
    {
        var preference = await GetByUserAndChannelAsync(userId, channel);
        if (preference != null)
        {
            _context.Set<UserNotificationPreference>().Remove(preference);
            await _context.SaveChangesAsync();
        }
    }
}
