using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;
using System.Text.Json;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Registry of notification topics with metadata for documentation and flow visualization.
/// Topics define what events a service can publish for notification routing.
/// </summary>
public class Topic
{
    public Guid Id { get; set; }

    /// <summary>
    /// The source service that publishes this topic
    /// </summary>
    public SourceService Service { get; set; }

    /// <summary>
    /// The notification topic identifier
    /// </summary>
    public NotificationTopic TopicName { get; set; }

    /// <summary>
    /// Human-readable display name for the topic
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what this topic represents
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Human-readable description of when/why this topic fires
    /// Example: "Triggered when: Workflow reaches Complete state with no errors"
    /// </summary>
    public string? TriggerDescription { get; set; }

    /// <summary>
    /// JSON Schema defining the event payload structure.
    /// Used for template variable documentation.
    /// </summary>
    public Dictionary<string, JsonElement>? PayloadSchema { get; set; }

    /// <summary>
    /// URL to external documentation for this topic
    /// </summary>
    public string? DocsUrl { get; set; }

    /// <summary>
    /// Whether this topic is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
