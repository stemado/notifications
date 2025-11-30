namespace NotificationService.Client.Models;

/// <summary>
/// Represents an actionable button or link in a notification
/// </summary>
public class NotificationAction
{
    /// <summary>
    /// Display label for the action (e.g., "View Details", "Dismiss")
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// URL or route to navigate to when action is clicked
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Action type for client handling
    /// </summary>
    public string ActionType { get; set; } = "link";

    /// <summary>
    /// Additional metadata for the action
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
