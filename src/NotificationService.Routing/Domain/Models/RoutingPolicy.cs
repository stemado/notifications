using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Defines who receives what notifications for which service/client combination.
/// Policies can be client-specific or default (applies when no client-specific policy exists).
/// </summary>
public class RoutingPolicy
{
    public Guid Id { get; set; }

    // Match criteria
    /// <summary>
    /// The source service this policy applies to
    /// </summary>
    public SourceService Service { get; set; }

    /// <summary>
    /// The notification topic this policy applies to
    /// </summary>
    public NotificationTopic Topic { get; set; }

    /// <summary>
    /// Client ID this policy applies to. Null means default policy for all clients without specific override.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Optional minimum severity filter. Only notifications at or above this severity are routed.
    /// </summary>
    public NotificationSeverity? MinSeverity { get; set; }

    // Delivery configuration
    /// <summary>
    /// The channel to use for delivery (Email, SMS, etc.)
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// The recipient group to send to
    /// </summary>
    public Guid RecipientGroupId { get; set; }

    /// <summary>
    /// The role for recipients (To, CC, BCC)
    /// </summary>
    public DeliveryRole Role { get; set; }

    // Control
    /// <summary>
    /// Whether this policy is active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Priority for conflict resolution. Higher = evaluated first.
    /// </summary>
    public int Priority { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public RecipientGroup? RecipientGroup { get; set; }
}
