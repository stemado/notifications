using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// A named collection of contacts for routing purposes.
/// Groups can be client-specific or global (available to all clients).
/// </summary>
public class RecipientGroup
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique name for the group (e.g., "HenryCounty-BrokerTeam", "Internal-CensusOps")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Client ID this group belongs to. Null means global group available for any client.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Description of the group's purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Purpose of the group - determines if it can be used for test emails, production, or both
    /// </summary>
    public GroupPurpose Purpose { get; set; } = GroupPurpose.Production;

    /// <summary>
    /// Tags for filtering and categorization (e.g., "client-testing", "internal", "qa")
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether the group is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public List<GroupMembership> Memberships { get; set; } = new();
    public List<RoutingPolicy> Policies { get; set; } = new();
}
