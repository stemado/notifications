using System.Text.Json;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Tracks test email deliveries for audit and history purposes.
/// Records who sent what test email to which recipients and when.
/// </summary>
public class TestEmailDelivery
{
    public Guid Id { get; set; }

    /// <summary>
    /// Optional reference to the recipient group used (null if ad-hoc contacts).
    /// For legacy single-group sends, this is the group used.
    /// For role-based sends, use ToGroupId/CcGroupId/BccGroupId instead.
    /// </summary>
    public Guid? RecipientGroupId { get; set; }

    /// <summary>
    /// Group ID for TO recipients (role-based sending)
    /// </summary>
    public Guid? ToGroupId { get; set; }

    /// <summary>
    /// Group ID for CC recipients (role-based sending)
    /// </summary>
    public Guid? CcGroupId { get; set; }

    /// <summary>
    /// Group ID for BCC recipients (role-based sending)
    /// </summary>
    public Guid? BccGroupId { get; set; }

    /// <summary>
    /// Name of the email template used
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Rendered subject line of the email
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// List of recipient email addresses (all recipients for legacy, TO only for role-based)
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// List of TO recipient email addresses (role-based sending)
    /// </summary>
    public List<string> ToRecipients { get; set; } = new();

    /// <summary>
    /// List of CC recipient email addresses (role-based sending)
    /// </summary>
    public List<string> CcRecipients { get; set; } = new();

    /// <summary>
    /// List of BCC recipient email addresses (role-based sending)
    /// </summary>
    public List<string> BccRecipients { get; set; } = new();

    /// <summary>
    /// Whether this test email used role-based TO/CC/BCC sending
    /// </summary>
    public bool UsedRoleBasedSending { get; set; }

    /// <summary>
    /// Reason/notes for sending this test email (for audit trail)
    /// </summary>
    public string? TestReason { get; set; }

    /// <summary>
    /// User or service that initiated the test email
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the test email was sent
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Whether the email send was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the send failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Message ID from the email provider (if successful)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Email provider used (e.g., "Resend", "SMTP")
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Additional metadata about the test email (template data, etc.)
    /// </summary>
    public Dictionary<string, JsonElement> Metadata { get; set; } = new();

    // Navigation
    public RecipientGroup? RecipientGroup { get; set; }
    public RecipientGroup? ToGroup { get; set; }
    public RecipientGroup? CcGroup { get; set; }
    public RecipientGroup? BccGroup { get; set; }
}
