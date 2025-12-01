namespace NotificationService.Domain.Models;

/// <summary>
/// Email template entity for storing and managing notification templates.
/// Templates use Liquid/Jinja2-compatible syntax via Scriban engine.
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// Primary key - auto-generated sequential ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique template name (e.g., "daily_import_summary", "escalation_alert")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the template's purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Email subject line (supports template variables)
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML body content with template variables
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Plain text body content with template variables (fallback for non-HTML clients)
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// JSON object defining template variables and their descriptions
    /// Format: {"ClientName": "The client's display name", "ImportDate": "Date of import"}
    /// </summary>
    public string? Variables { get; set; }

    /// <summary>
    /// JSON object with sample data for template preview/testing
    /// </summary>
    public string? TestData { get; set; }

    /// <summary>
    /// JSON array of default recipient email addresses
    /// </summary>
    public string? DefaultRecipients { get; set; }

    /// <summary>
    /// Template category (e.g., "notification", "success", "error", "escalation")
    /// </summary>
    public string TemplateType { get; set; } = "notification";

    /// <summary>
    /// Whether this template is currently active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
