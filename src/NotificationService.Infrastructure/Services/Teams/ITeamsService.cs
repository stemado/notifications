namespace NotificationService.Infrastructure.Services.Teams;

/// <summary>
/// Service for sending messages to Microsoft Teams
/// </summary>
public interface ITeamsService
{
    /// <summary>
    /// Sends a message to a Teams channel via webhook
    /// </summary>
    Task<bool> SendMessageAsync(string webhookUrl, object message);
}
