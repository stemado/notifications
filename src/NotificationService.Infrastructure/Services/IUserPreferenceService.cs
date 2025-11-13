using NotificationService.Domain.Enums;
using NotificationService.Domain.Models.Preferences;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Service for managing user notification preferences (Phase 2)
/// </summary>
public interface IUserPreferenceService
{
    /// <summary>
    /// Gets all preferences for a user
    /// </summary>
    Task<List<UserNotificationPreference>> GetUserPreferencesAsync(Guid userId);

    /// <summary>
    /// Gets preference for a specific user and channel
    /// </summary>
    Task<UserNotificationPreference?> GetPreferenceAsync(Guid userId, NotificationChannel channel);

    /// <summary>
    /// Creates or updates a preference
    /// </summary>
    Task<UserNotificationPreference> SetPreferenceAsync(Guid userId, NotificationChannel channel, NotificationSeverity minSeverity, bool enabled);

    /// <summary>
    /// Checks if a channel is enabled for a user with the given notification severity
    /// </summary>
    Task<bool> IsChannelEnabledAsync(Guid userId, NotificationChannel channel, NotificationSeverity severity);

    /// <summary>
    /// Deletes a preference
    /// </summary>
    Task DeletePreferenceAsync(Guid userId, NotificationChannel channel);

    /// <summary>
    /// Sets default preferences for a new user
    /// </summary>
    Task SetDefaultPreferencesAsync(Guid userId);
}
