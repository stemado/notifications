using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// Request to create a new recipient group
/// </summary>
public record CreateRecipientGroupRequest
{
    public required string Name { get; init; }
    public string? ClientId { get; init; }
    public string? Description { get; init; }
    public GroupPurpose Purpose { get; init; } = GroupPurpose.Production;
    public List<string>? Tags { get; init; }
}

/// <summary>
/// Request to update an existing recipient group
/// </summary>
public record UpdateRecipientGroupRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    /// <summary>
    /// If null, IsActive is not changed. If true/false, updates the active status.
    /// </summary>
    public bool? IsActive { get; init; }
    public GroupPurpose? Purpose { get; init; }
    public List<string>? Tags { get; init; }
}

/// <summary>
/// Request to add a member to a group
/// </summary>
public record AddGroupMemberRequest
{
    public Guid ContactId { get; init; }
}

/// <summary>
/// Summary view of a recipient group
/// </summary>
public record RecipientGroupSummary
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ClientId { get; init; }
    public string? Description { get; init; }
    public GroupPurpose Purpose { get; init; }
    public List<string> Tags { get; init; } = new();
    public bool IsActive { get; init; }
    public int MemberCount { get; init; }
    public int PolicyCount { get; init; }
}

/// <summary>
/// Detailed view of a recipient group with members
/// </summary>
public record RecipientGroupDetails
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ClientId { get; init; }
    public string? Description { get; init; }
    public GroupPurpose Purpose { get; init; }
    public List<string> Tags { get; init; } = new();
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<GroupMemberInfo> Members { get; init; } = new();
    public List<PolicySummaryForGroup> Policies { get; init; } = new();
}

/// <summary>
/// Member info for a group
/// </summary>
public record GroupMemberInfo
{
    public Guid ContactId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Organization { get; init; }
    public bool IsActive { get; init; }
    public DateTime AddedAt { get; init; }
    public string? AddedBy { get; init; }
}

/// <summary>
/// Policy summary shown in group details
/// </summary>
public record PolicySummaryForGroup
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string Topic { get; init; }
    public required string Channel { get; init; }
    public required string Role { get; init; }
    public bool IsEnabled { get; init; }
}
