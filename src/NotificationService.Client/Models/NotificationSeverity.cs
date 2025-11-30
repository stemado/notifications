namespace NotificationService.Client.Models;

/// <summary>
/// Notification severity levels matching NotificationService.Domain
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Informational notification
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning notification
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Urgent notification requiring attention
    /// </summary>
    Urgent = 2,

    /// <summary>
    /// Critical notification requiring immediate action
    /// </summary>
    Critical = 3
}
