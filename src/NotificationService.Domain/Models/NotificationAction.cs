namespace NotificationService.Domain.Models;

/// <summary>
/// Represents an action that can be performed on a notification
/// </summary>
public class NotificationAction
{
    /// <summary>
    /// Display label for the action button
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Type of action: navigate, api_call, dismiss
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Target for the action (URL for navigate, endpoint for api_call)
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Visual variant: primary, secondary, danger
    /// </summary>
    public string Variant { get; set; } = "secondary";
}
