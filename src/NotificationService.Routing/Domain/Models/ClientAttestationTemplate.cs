namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Links a client to an attestation email template with enable/disable status.
/// This allows per-client configuration of which attestation templates are active.
/// </summary>
public class ClientAttestationTemplate
{
    public Guid Id { get; set; }

    /// <summary>
    /// The client this configuration applies to
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the email template (from email_templates table)
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Whether this template is enabled for the client
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Priority for ordering templates in UI. Higher = shown first.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Optional notes about this configuration
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public List<ClientAttestationTemplatePolicy> Policies { get; set; } = new();
    public List<ClientAttestationTemplateGroup> Groups { get; set; } = new();
}
