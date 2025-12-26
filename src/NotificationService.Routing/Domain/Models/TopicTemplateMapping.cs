using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.Domain.Models;

/// <summary>
/// Maps service/topic combinations to email templates for automatic template resolution.
/// When an OutboundEvent is published without a TemplateId, this mapping is used
/// to automatically select and render the appropriate template.
/// </summary>
public class TopicTemplateMapping
{
    public Guid Id { get; set; }

    /// <summary>
    /// The source service publishing notifications (e.g., CensusReconciliation, CensusOrchestration)
    /// </summary>
    public SourceService Service { get; set; }

    /// <summary>
    /// The notification topic (e.g., DailyImportSuccess, ReconciliationComplete)
    /// </summary>
    public NotificationTopic Topic { get; set; }

    /// <summary>
    /// Optional client-specific override. If null, this is the default mapping for all clients.
    /// Client-specific mappings take priority over default mappings.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The email template ID to use (references email_templates.id)
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Whether this mapping is active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Priority for resolution when multiple mappings match (higher wins)
    /// </summary>
    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
