namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Links an enabled client attestation template to a routing policy.
/// Multiple policies can be assigned to determine which recipients get the email.
/// </summary>
public class ClientAttestationTemplatePolicy
{
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent client-template association
    /// </summary>
    public Guid ClientAttestationTemplateId { get; set; }

    /// <summary>
    /// Reference to the routing policy that determines recipients
    /// </summary>
    public Guid RoutingPolicyId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Navigation properties
    public ClientAttestationTemplate? ClientAttestationTemplate { get; set; }
    public RoutingPolicy? RoutingPolicy { get; set; }
}
