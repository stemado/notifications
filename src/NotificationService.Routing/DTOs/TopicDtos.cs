using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;
using System.Text.Json;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// Request to create a new topic registry entry
/// </summary>
public record CreateTopicRequest
{
    public required SourceService Service { get; init; }
    public required NotificationTopic TopicName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? TriggerDescription { get; init; }
    public Dictionary<string, JsonElement>? PayloadSchema { get; init; }
    public string? DocsUrl { get; init; }
}

/// <summary>
/// Request to update an existing topic registry entry
/// </summary>
public record UpdateTopicRequest
{
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? TriggerDescription { get; init; }
    public Dictionary<string, JsonElement>? PayloadSchema { get; init; }
    public string? DocsUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Summary view of a topic
/// </summary>
public record TopicSummary
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string TopicName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Detailed view of a topic including schema and trigger info
/// </summary>
public record TopicDetails
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string TopicName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? TriggerDescription { get; init; }
    public Dictionary<string, JsonElement>? PayloadSchema { get; init; }
    public string? DocsUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
