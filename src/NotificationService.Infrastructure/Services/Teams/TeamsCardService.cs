using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Teams;

/// <summary>
/// Service for creating Teams Adaptive Cards from notifications
/// </summary>
public class TeamsCardService : ITeamsCardService
{
    public object CreateAdaptiveCard(Notification notification)
    {
        var color = GetSeverityColor(notification.Severity);
        var icon = GetSeverityIcon(notification.Severity);

        // Build Adaptive Card (https://adaptivecards.io/)
        var card = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new List<object>
                        {
                            // Header with severity
                            new
                            {
                                type = "Container",
                                style = "emphasis",
                                items = new[]
                                {
                                    new
                                    {
                                        type = "TextBlock",
                                        text = $"{icon} **{notification.Severity}** Notification",
                                        size = "Large",
                                        weight = "Bolder",
                                        color = color
                                    }
                                }
                            },
                            // Title
                            new
                            {
                                type = "TextBlock",
                                text = notification.Title,
                                size = "ExtraLarge",
                                weight = "Bolder",
                                wrap = true
                            },
                            // Message
                            new
                            {
                                type = "TextBlock",
                                text = notification.Message,
                                wrap = true,
                                spacing = "Medium"
                            },
                            // Metadata (if any)
                            notification.Metadata.Any() ? new
                            {
                                type = "FactSet",
                                facts = notification.Metadata.Select(kvp => new
                                {
                                    title = kvp.Key,
                                    value = kvp.Value.ToString()
                                }).ToArray()
                            } : null,
                            // Footer
                            new
                            {
                                type = "TextBlock",
                                text = $"Created at {notification.CreatedAt:f}",
                                size = "Small",
                                isSubtle = true,
                                spacing = "Medium"
                            }
                        }.Where(x => x != null).ToList(),
                        actions = notification.Actions.Select(action => new
                        {
                            type = "Action.OpenUrl",
                            title = action.Label,
                            url = action.Target
                        }).ToArray()
                    }
                }
            }
        };

        return card;
    }

    private string GetSeverityColor(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => "Attention",
            NotificationSeverity.Urgent => "Warning",
            NotificationSeverity.Warning => "Warning",
            NotificationSeverity.Info => "Good",
            _ => "Default"
        };
    }

    private string GetSeverityIcon(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Critical => "🚨",
            NotificationSeverity.Urgent => "⚠️",
            NotificationSeverity.Warning => "⚡",
            NotificationSeverity.Info => "ℹ️",
            _ => "📢"
        };
    }
}
