using System.Text.Json;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// Complete flow data for visualizing a policy's notification path
/// </summary>
public record FlowData
{
    /// <summary>
    /// Topic metadata (may be null if topic not registered)
    /// </summary>
    public TopicInfo? Topic { get; init; }

    /// <summary>
    /// Template mapping information
    /// </summary>
    public TemplateMappingInfo? TemplateMapping { get; init; }

    /// <summary>
    /// The channel for this flow (Email, SMS, etc.)
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    /// All related policies that fire together (same service/topic/client/channel)
    /// </summary>
    public required List<FlowPolicyInfo> Policies { get; init; }

    /// <summary>
    /// Recipient groups with member details
    /// </summary>
    public required List<FlowRecipientGroupInfo> RecipientGroups { get; init; }
}

/// <summary>
/// Topic information for flow visualization
/// </summary>
public record TopicInfo
{
    public required string Service { get; init; }
    public required string TopicName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? TriggerDescription { get; init; }
    public Dictionary<string, JsonElement>? PayloadSchema { get; init; }
    public string? DocsUrl { get; init; }
}

/// <summary>
/// Template mapping info for flow visualization
/// </summary>
public record TemplateMappingInfo
{
    public int TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string Subject { get; init; }
}

/// <summary>
/// Policy info for flow visualization
/// </summary>
public record FlowPolicyInfo
{
    public Guid Id { get; init; }
    public required string Role { get; init; }
    public Guid RecipientGroupId { get; init; }
    public bool IsEnabled { get; init; }
    /// <summary>
    /// True if this is the policy being viewed
    /// </summary>
    public bool IsCurrent { get; init; }
}

/// <summary>
/// Recipient group info for flow visualization
/// </summary>
public record FlowRecipientGroupInfo
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public int MemberCount { get; init; }
    public required List<string> Members { get; init; }
}

/// <summary>
/// Request to simulate notification routing
/// </summary>
public record SimulateFlowRequest
{
    public required string Service { get; init; }
    public required string TopicName { get; init; }
    public string? ClientId { get; init; }
}
