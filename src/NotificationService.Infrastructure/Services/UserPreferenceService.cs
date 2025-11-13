using NotificationService.Domain.Enums;
using NotificationService.Domain.Models.Preferences;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Service implementation for managing user notification preferences
/// </summary>
public class UserPreferenceService : IUserPreferenceService
{
    private readonly IUserPreferenceRepository _repository;

    public UserPreferenceService(IUserPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UserNotificationPreference>> GetUserPreferencesAsync(Guid userId)
    {
        var preferences = await _repository.GetByUserIdAsync(userId);

        // If no preferences exist, return defaults
        if (preferences.Count == 0)
        {
            return GetDefaultPreferences(userId);
        }

        return preferences;
    }

    public async Task<UserNotificationPreference?> GetPreferenceAsync(Guid userId, NotificationChannel channel)
    {
        var preference = await _repository.GetByUserAndChannelAsync(userId, channel);

        // If no preference exists, return default
        if (preference == null)
        {
            return GetDefaultPreference(userId, channel);
        }

        return preference;
    }

    public async Task<UserNotificationPreference> SetPreferenceAsync(
        Guid userId,
        NotificationChannel channel,
        NotificationSeverity minSeverity,
        bool enabled)
    {
        var existing = await _repository.GetByUserAndChannelAsync(userId, channel);

        if (existing != null)
        {
            existing.MinSeverity = minSeverity;
            existing.Enabled = enabled;
            await _repository.UpdateAsync(existing);
            return existing;
        }

        var preference = new UserNotificationPreference
        {
            UserId = userId,
            Channel = channel,
            MinSeverity = minSeverity,
            Enabled = enabled
        };

        return await _repository.CreateAsync(preference);
    }

    public async Task<bool> IsChannelEnabledAsync(Guid userId, NotificationChannel channel, NotificationSeverity severity)
    {
        var preference = await GetPreferenceAsync(userId, channel);

        if (preference == null)
        {
            return false;
        }

        // Check if channel is enabled and severity meets minimum
        return preference.Enabled && severity >= preference.MinSeverity;
    }

    public async Task DeletePreferenceAsync(Guid userId, NotificationChannel channel)
    {
        await _repository.DeleteAsync(userId, channel);
    }

    public async Task SetDefaultPreferencesAsync(Guid userId)
    {
        var defaults = GetDefaultPreferences(userId);

        foreach (var preference in defaults)
        {
            await _repository.CreateAsync(preference);
        }
    }

    private List<UserNotificationPreference> GetDefaultPreferences(Guid userId)
    {
        return new List<UserNotificationPreference>
        {
            // SignalR enabled for all severities (real-time)
            new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.SignalR,
                MinSeverity = NotificationSeverity.Info,
                Enabled = true
            },
            // Email enabled for Warning and above
            new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.Email,
                MinSeverity = NotificationSeverity.Warning,
                Enabled = true
            },
            // SMS enabled for Critical only
            new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.SMS,
                MinSeverity = NotificationSeverity.Critical,
                Enabled = false // Disabled by default (cost)
            },
            // Slack enabled for Urgent and above
            new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.Slack,
                MinSeverity = NotificationSeverity.Urgent,
                Enabled = false // Disabled by default
            }
        };
    }

    private UserNotificationPreference GetDefaultPreference(Guid userId, NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.SignalR => new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.SignalR,
                MinSeverity = NotificationSeverity.Info,
                Enabled = true
            },
            NotificationChannel.Email => new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.Email,
                MinSeverity = NotificationSeverity.Warning,
                Enabled = true
            },
            NotificationChannel.SMS => new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.SMS,
                MinSeverity = NotificationSeverity.Critical,
                Enabled = false
            },
            NotificationChannel.Slack => new UserNotificationPreference
            {
                UserId = userId,
                Channel = NotificationChannel.Slack,
                MinSeverity = NotificationSeverity.Urgent,
                Enabled = false
            },
            _ => throw new ArgumentException($"Unknown channel: {channel}")
        };
    }
}
