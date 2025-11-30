namespace NotificationService.Client.Models;

/// <summary>
/// Response from the NotificationService API after creating/updating a notification
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The notification ID (Guid) if successful
    /// </summary>
    public Guid? NotificationId { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// HTTP status code from the response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Whether this was an update to an existing notification (via GroupKey)
    /// </summary>
    public bool WasUpdated { get; set; }

    /// <summary>
    /// Timestamp when the notification was created/updated
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static NotificationResponse Successful(Guid notificationId, bool wasUpdated = false) => new()
    {
        Success = true,
        NotificationId = notificationId,
        StatusCode = wasUpdated ? 200 : 201,
        WasUpdated = wasUpdated,
        Timestamp = DateTime.UtcNow
    };

    /// <summary>
    /// Creates a failed response
    /// </summary>
    public static NotificationResponse Failed(string errorMessage, int statusCode = 500) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        StatusCode = statusCode,
        Timestamp = DateTime.UtcNow
    };
}
