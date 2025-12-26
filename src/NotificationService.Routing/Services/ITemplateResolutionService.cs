using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// Result of template resolution containing rendered email content
/// </summary>
public record ResolvedEmailContent(
    string Subject,
    string? HtmlBody,
    string? PlainTextBody,
    int? TemplateId);

/// <summary>
/// Service for resolving email templates based on service/topic combinations.
/// Automatically looks up the appropriate template and renders it with event payload.
/// </summary>
public interface ITemplateResolutionService
{
    /// <summary>
    /// Resolve and render email content for an outbound event.
    ///
    /// Resolution priority:
    /// 1. If evt.TemplateId is set, use that template directly
    /// 2. Look up mapping by Service/Topic/ClientId (client-specific first, then default)
    /// 3. If no mapping found, fall back to evt.Subject and evt.Body
    /// </summary>
    /// <param name="evt">The outbound event containing service, topic, and payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved email content with rendered subject and bodies</returns>
    Task<ResolvedEmailContent> ResolveEmailContentAsync(
        OutboundEvent evt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a template mapping exists for the given service/topic combination
    /// </summary>
    Task<bool> HasMappingAsync(
        Domain.Enums.SourceService service,
        Domain.Enums.NotificationTopic topic,
        string? clientId,
        CancellationToken cancellationToken = default);
}
