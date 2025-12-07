namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// A person who can receive notifications. Not necessarily a system user.
/// External contacts like brokers, client HR contacts, etc.
/// </summary>
public class Contact
{
    public Guid Id { get; set; }

    /// <summary>
    /// Display name for the contact
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary email address for notifications
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number for SMS notifications
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Organization/company the contact belongs to (e.g., "ABC Broker Agency")
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Whether the contact is active and should receive notifications
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional link to identity when/if they get portal access
    /// </summary>
    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>
    /// Internal notes about the contact
    /// </summary>
    public string? Notes { get; set; }

    // Navigation
    public List<GroupMembership> Memberships { get; set; } = new();
}
