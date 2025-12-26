using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services.Templates;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service for resolving email templates based on service/topic combinations.
/// Automatically looks up the appropriate template and renders it with event payload.
/// </summary>
public class TemplateResolutionService : ITemplateResolutionService
{
    private readonly ITopicTemplateMappingRepository _mappingRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly ITemplateRenderingService _renderingService;
    private readonly ILogger<TemplateResolutionService> _logger;

    public TemplateResolutionService(
        ITopicTemplateMappingRepository mappingRepository,
        IEmailTemplateRepository templateRepository,
        ITemplateRenderingService renderingService,
        ILogger<TemplateResolutionService> logger)
    {
        _mappingRepository = mappingRepository;
        _templateRepository = templateRepository;
        _renderingService = renderingService;
        _logger = logger;
    }

    public async Task<ResolvedEmailContent> ResolveEmailContentAsync(
        OutboundEvent evt,
        CancellationToken cancellationToken = default)
    {
        // Priority 1: If explicit TemplateId is provided, use it directly
        if (!string.IsNullOrEmpty(evt.TemplateId) && int.TryParse(evt.TemplateId, out var explicitTemplateId))
        {
            _logger.LogDebug(
                "Using explicit TemplateId {TemplateId} for event {EventId}",
                explicitTemplateId, evt.Id);

            return await RenderTemplateAsync(explicitTemplateId, evt.Payload, cancellationToken);
        }

        // Priority 2: Look up mapping by Service/Topic/ClientId
        var mapping = await _mappingRepository.GetMappingAsync(evt.Service, evt.Topic, evt.ClientId);

        if (mapping != null)
        {
            _logger.LogDebug(
                "Found template mapping for {Service}/{Topic}/{ClientId} -> TemplateId {TemplateId}",
                evt.Service, evt.Topic, evt.ClientId ?? "(default)", mapping.TemplateId);

            return await RenderTemplateAsync(mapping.TemplateId, evt.Payload, cancellationToken);
        }

        // Priority 3: Fall back to event Subject/Body (no template)
        _logger.LogDebug(
            "No template mapping found for {Service}/{Topic}, using event Subject/Body",
            evt.Service, evt.Topic);

        return new ResolvedEmailContent(
            Subject: evt.Subject ?? "Notification",
            HtmlBody: evt.Body,
            PlainTextBody: null,
            TemplateId: null);
    }

    public async Task<bool> HasMappingAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await _mappingRepository.GetMappingAsync(service, topic, clientId);
        return mapping != null;
    }

    private async Task<ResolvedEmailContent> RenderTemplateAsync(
        int templateId,
        Dictionary<string, JsonElement> payload,
        CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException(
                $"Email template {templateId} not found");
        }

        if (!template.IsActive)
        {
            throw new InvalidOperationException(
                $"Email template {templateId} ({template.Name}) is not active");
        }

        // Convert JsonElement dictionary to object dictionary for Scriban
        var data = ConvertPayloadToTemplateDictionary(payload);

        // Render subject
        var subject = _renderingService.RenderTemplate(template.Subject, data);

        // Render HTML body if present
        string? htmlBody = null;
        if (!string.IsNullOrEmpty(template.HtmlContent))
        {
            htmlBody = _renderingService.RenderTemplate(template.HtmlContent, data);
        }

        // Render plain text body if present
        string? plainTextBody = null;
        if (!string.IsNullOrEmpty(template.TextContent))
        {
            plainTextBody = _renderingService.RenderTemplate(template.TextContent, data);
        }

        _logger.LogInformation(
            "Rendered template {TemplateId} ({TemplateName}) for notification",
            templateId, template.Name);

        return new ResolvedEmailContent(
            Subject: subject,
            HtmlBody: htmlBody,
            PlainTextBody: plainTextBody,
            TemplateId: templateId);
    }

    private static Dictionary<string, object?> ConvertPayloadToTemplateDictionary(
        Dictionary<string, JsonElement> payload)
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in payload)
        {
            result[kvp.Key] = ConvertJsonElement(kvp.Value);
        }

        return result;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }
}
