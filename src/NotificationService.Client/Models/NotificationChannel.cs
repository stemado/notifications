namespace NotificationService.Client.Models;

/// <summary>
/// Available notification delivery channels
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Real-time notification via SignalR
    /// </summary>
    SignalR = 0,

    /// <summary>
    /// Email notification
    /// </summary>
    Email = 1,

    /// <summary>
    /// SMS notification
    /// </summary>
    SMS = 2,

    /// <summary>
    /// Microsoft Teams notification
    /// </summary>
    Teams = 3,

    /// <summary>
    /// Webhook notification
    /// </summary>
    Webhook = 4
}
