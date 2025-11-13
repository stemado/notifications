namespace NotificationService.Domain.Enums;

/// <summary>
/// Defines the available notification delivery channels (Phase 2)
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Real-time notification via SignalR
    /// </summary>
    SignalR,

    /// <summary>
    /// Email notification
    /// </summary>
    Email,

    /// <summary>
    /// SMS notification
    /// </summary>
    SMS,

    /// <summary>
    /// Slack notification
    /// </summary>
    Slack
}
