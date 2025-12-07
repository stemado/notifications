namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Junction table linking contacts to groups.
/// </summary>
public class GroupMembership
{
    public Guid GroupId { get; set; }
    public Guid ContactId { get; set; }

    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Who added this contact to the group
    /// </summary>
    public string? AddedBy { get; set; }

    // Navigation
    public RecipientGroup? Group { get; set; }
    public Contact? Contact { get; set; }
}
