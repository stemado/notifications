namespace NotificationService.Domain.Enums;

/// <summary>
/// Defines the severity levels for notifications
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Informational notification
    /// </summary>
    Info,

    /// <summary>
    /// Warning notification
    /// </summary>
    Warning,

    /// <summary>
    /// Urgent notification requiring attention
    /// </summary>
    Urgent,

    /// <summary>
    /// Critical notification requiring immediate action
    /// </summary>
    Critical
}
