namespace NotificationService.Routing.DTOs;

/// <summary>
/// Request to create a new contact
/// </summary>
public record CreateContactRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Organization { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Request to update an existing contact
/// </summary>
public record UpdateContactRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Organization { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Summary view of a contact
/// </summary>
public record ContactSummary
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Organization { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int GroupCount { get; init; }
}

/// <summary>
/// Detailed view of a contact including group memberships
/// </summary>
public record ContactDetails
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Organization { get; init; }
    public bool IsActive { get; init; }
    public Guid? UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeactivatedAt { get; init; }
    public string? Notes { get; init; }
    public List<GroupMembershipInfo> Groups { get; init; } = new();
}

/// <summary>
/// Group membership info for a contact
/// </summary>
public record GroupMembershipInfo
{
    public Guid GroupId { get; init; }
    public required string GroupName { get; init; }
    public string? ClientId { get; init; }
    public DateTime AddedAt { get; init; }
}
