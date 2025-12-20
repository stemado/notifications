using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Links an enabled client attestation template directly to a recipient group.
/// Alternative to policies for simpler routing scenarios.
/// </summary>
public class ClientAttestationTemplateGroup
{
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent client-template association
    /// </summary>
    public Guid ClientAttestationTemplateId { get; set; }

    /// <summary>
    /// Reference to the recipient group
    /// </summary>
    public Guid RecipientGroupId { get; set; }

    /// <summary>
    /// The delivery role for this group (To, Cc, Bcc)
    /// </summary>
    public DeliveryRole Role { get; set; } = DeliveryRole.To;

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Navigation properties
    public ClientAttestationTemplate? ClientAttestationTemplate { get; set; }
    public RecipientGroup? RecipientGroup { get; set; }
}
