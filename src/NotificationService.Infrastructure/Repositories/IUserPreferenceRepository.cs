using NotificationService.Domain.Enums;
using NotificationService.Domain.Models.Preferences;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository for user notification preferences
/// </summary>
public interface IUserPreferenceRepository
{
    Task<List<UserNotificationPreference>> GetByUserIdAsync(Guid userId);
    Task<UserNotificationPreference?> GetByUserAndChannelAsync(Guid userId, NotificationChannel channel);
    Task<UserNotificationPreference> CreateAsync(UserNotificationPreference preference);
    Task UpdateAsync(UserNotificationPreference preference);
    Task DeleteAsync(Guid userId, NotificationChannel channel);
}
