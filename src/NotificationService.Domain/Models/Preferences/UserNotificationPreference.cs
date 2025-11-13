using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Models.Preferences;

/// <summary>
/// User preferences for notification channels (Phase 2)
/// </summary>
public class UserNotificationPreference
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Notification channel
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Minimum severity level for this channel (only notify if notification severity >= this)
    /// </summary>
    public NotificationSeverity MinSeverity { get; set; }

    /// <summary>
    /// Whether this channel is enabled for the user
    /// </summary>
    public bool Enabled { get; set; } = true;
}
