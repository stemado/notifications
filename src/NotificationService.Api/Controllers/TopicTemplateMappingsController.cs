using Microsoft.AspNetCore.Mvc;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Repositories;
using NotificationService.Routing.Services;

namespace NotificationService.Api.Controllers;

/// <summary>
/// API controller for managing topic-to-template mappings.
/// Mappings determine which email template to use when services publish events with specific topics.
/// </summary>
[ApiController]
[Route("api/topic-template-mappings")]
[Produces("application/json")]
public class TopicTemplateMappingsController : ControllerBase
{
    private readonly ITopicTemplateMappingRepository _mappingRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly ITemplateResolutionService _resolutionService;
    private readonly ILogger<TopicTemplateMappingsController> _logger;

    public TopicTemplateMappingsController(
        ITopicTemplateMappingRepository mappingRepository,
        IEmailTemplateRepository templateRepository,
        ITemplateResolutionService resolutionService,
        ILogger<TopicTemplateMappingsController> logger)
    {
        _mappingRepository = mappingRepository;
        _templateRepository = templateRepository;
        _resolutionService = resolutionService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/topic-template-mappings - Get all active mappings
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TopicTemplateMappingListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMappings([FromQuery] bool includeDisabled = false)
    {
        var mappings = await _mappingRepository.GetAllAsync(includeDisabled);

        // Get template names for display
        var templateIds = mappings.Select(m => m.TemplateId).Distinct().ToList();
        var templateNames = new Dictionary<int, string>();

        foreach (var templateId in templateIds)
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template != null)
            {
                templateNames[templateId] = template.Name;
            }
        }

        var dtos = mappings.Select(m => MapToDto(m, templateNames)).ToList();

        _logger.LogInformation("Retrieved {Count} topic-template mappings", mappings.Count);
        return Ok(new TopicTemplateMappingListResponse(dtos.Count, dtos));
    }

    /// <summary>
    /// GET /api/topic-template-mappings/{id} - Get a specific mapping by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TopicTemplateMappingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappingById(Guid id)
    {
        var mapping = await _mappingRepository.GetByIdAsync(id);
        if (mapping == null)
        {
            _logger.LogWarning("Mapping not found: {MappingId}", id);
            return NotFound(new { message = $"Mapping with ID {id} not found" });
        }

        var template = await _templateRepository.GetByIdAsync(mapping.TemplateId);
        var dto = MapToDto(mapping, template?.Name);

        _logger.LogInformation("Retrieved mapping: {MappingId}", id);
        return Ok(dto);
    }

    /// <summary>
    /// GET /api/topic-template-mappings/template/{templateId} - Get all mappings for a specific template
    /// </summary>
    [HttpGet("template/{templateId:int}")]
    [ProducesResponseType(typeof(TopicTemplateMappingListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMappingsByTemplate(int templateId)
    {
        var mappings = await _mappingRepository.GetByTemplateIdAsync(templateId);
        var template = await _templateRepository.GetByIdAsync(templateId);
        var templateName = template?.Name;

        var dtos = mappings.Select(m => MapToDto(m, templateName)).ToList();

        _logger.LogInformation("Retrieved {Count} mappings for template {TemplateId}", mappings.Count, templateId);
        return Ok(new TopicTemplateMappingListResponse(dtos.Count, dtos));
    }

    /// <summary>
    /// GET /api/topic-template-mappings/resolve - Preview which template would be resolved for a service/topic
    /// </summary>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(TemplateResolutionPreviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveTemplate(
        [FromQuery] SourceService service,
        [FromQuery] NotificationTopic topic,
        [FromQuery] string? clientId = null)
    {
        var mapping = await _mappingRepository.GetMappingAsync(service, topic, clientId);

        if (mapping == null)
        {
            return Ok(new TemplateResolutionPreviewResponse
            {
                Service = service,
                Topic = topic,
                ClientId = clientId,
                HasMapping = false,
                Mapping = null,
                TemplateName = null,
                TemplateSubject = null
            });
        }

        var template = await _templateRepository.GetByIdAsync(mapping.TemplateId);

        return Ok(new TemplateResolutionPreviewResponse
        {
            Service = service,
            Topic = topic,
            ClientId = clientId,
            HasMapping = true,
            Mapping = MapToDto(mapping, template?.Name),
            TemplateName = template?.Name,
            TemplateSubject = template?.Subject
        });
    }

    /// <summary>
    /// POST /api/topic-template-mappings - Create a new mapping
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TopicTemplateMappingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMapping([FromBody] CreateTopicTemplateMappingRequest request)
    {
        // Validate template exists
        var template = await _templateRepository.GetByIdAsync(request.TemplateId);
        if (template == null)
        {
            return BadRequest(new { message = $"Template with ID {request.TemplateId} not found" });
        }

        // Check for existing mapping with same service/topic/client
        var existingMapping = await _mappingRepository.GetMappingAsync(
            request.Service, request.Topic, request.ClientId);

        if (existingMapping != null && existingMapping.ClientId == request.ClientId)
        {
            return BadRequest(new
            {
                message = $"A mapping already exists for {request.Service}/{request.Topic}/{request.ClientId ?? "(default)"}. " +
                          $"Use PUT to update or DELETE first."
            });
        }

        var mapping = new TopicTemplateMapping
        {
            Id = Guid.NewGuid(),
            Service = request.Service,
            Topic = request.Topic,
            ClientId = request.ClientId,
            TemplateId = request.TemplateId,
            Priority = request.Priority,
            IsEnabled = request.IsEnabled
        };

        var created = await _mappingRepository.CreateAsync(mapping);
        var dto = MapToDto(created, template.Name);

        _logger.LogInformation(
            "Created topic-template mapping: {MappingId} ({Service}/{Topic} -> Template {TemplateId})",
            created.Id, created.Service, created.Topic, created.TemplateId);

        return CreatedAtAction(nameof(GetMappingById), new { id = created.Id }, dto);
    }

    /// <summary>
    /// PUT /api/topic-template-mappings/{id} - Update an existing mapping
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TopicTemplateMappingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMapping(Guid id, [FromBody] UpdateTopicTemplateMappingRequest request)
    {
        var existing = await _mappingRepository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Mapping not found for update: {MappingId}", id);
            return NotFound(new { message = $"Mapping with ID {id} not found" });
        }

        // Validate template exists
        var template = await _templateRepository.GetByIdAsync(request.TemplateId);
        if (template == null)
        {
            return BadRequest(new { message = $"Template with ID {request.TemplateId} not found" });
        }

        existing.TemplateId = request.TemplateId;
        if (request.Priority.HasValue)
            existing.Priority = request.Priority.Value;
        if (request.IsEnabled.HasValue)
            existing.IsEnabled = request.IsEnabled.Value;

        var updated = await _mappingRepository.UpdateAsync(existing);
        var dto = MapToDto(updated, template.Name);

        _logger.LogInformation(
            "Updated topic-template mapping: {MappingId} ({Service}/{Topic} -> Template {TemplateId})",
            updated.Id, updated.Service, updated.Topic, updated.TemplateId);

        return Ok(dto);
    }

    /// <summary>
    /// POST /api/topic-template-mappings/{id}/toggle - Toggle mapping enabled/disabled status
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(typeof(ToggleMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleMapping(Guid id)
    {
        try
        {
            var toggled = await _mappingRepository.ToggleAsync(id);

            _logger.LogInformation(
                "Toggled mapping {MappingId} to IsEnabled={IsEnabled}",
                id, toggled.IsEnabled);

            return Ok(new ToggleMappingResponse
            {
                Id = toggled.Id,
                IsEnabled = toggled.IsEnabled,
                UpdatedAt = toggled.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Mapping not found for toggle: {MappingId}", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/topic-template-mappings/{id} - Delete a mapping
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMapping(Guid id)
    {
        try
        {
            await _mappingRepository.DeleteAsync(id);

            _logger.LogInformation("Deleted topic-template mapping: {MappingId}", id);
            return Ok(new { message = $"Mapping {id} deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Mapping not found for deletion: {MappingId}", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/topic-template-mappings/services - Get list of available source services
    /// </summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(EnumValuesResponse), StatusCodes.Status200OK)]
    public IActionResult GetAvailableServices()
    {
        var values = Enum.GetValues<SourceService>()
            .Select(s => new EnumValueDto(s.ToString(), (int)s))
            .ToList();

        return Ok(new EnumValuesResponse("SourceService", values));
    }

    /// <summary>
    /// GET /api/topic-template-mappings/topics - Get list of available notification topics
    /// </summary>
    [HttpGet("topics")]
    [ProducesResponseType(typeof(EnumValuesResponse), StatusCodes.Status200OK)]
    public IActionResult GetAvailableTopics()
    {
        var values = Enum.GetValues<NotificationTopic>()
            .Select(t => new EnumValueDto(t.ToString(), (int)t))
            .ToList();

        return Ok(new EnumValuesResponse("NotificationTopic", values));
    }

    private static TopicTemplateMappingDto MapToDto(TopicTemplateMapping mapping, string? templateName)
    {
        return new TopicTemplateMappingDto
        {
            Id = mapping.Id,
            Service = mapping.Service,
            Topic = mapping.Topic,
            ClientId = mapping.ClientId,
            TemplateId = mapping.TemplateId,
            TemplateName = templateName,
            IsEnabled = mapping.IsEnabled,
            Priority = mapping.Priority,
            CreatedAt = mapping.CreatedAt,
            UpdatedAt = mapping.UpdatedAt,
            UpdatedBy = mapping.UpdatedBy
        };
    }

    private static TopicTemplateMappingDto MapToDto(
        TopicTemplateMapping mapping,
        Dictionary<int, string> templateNames)
    {
        templateNames.TryGetValue(mapping.TemplateId, out var templateName);
        return MapToDto(mapping, templateName);
    }
}

// Response DTOs specific to this controller

public record TopicTemplateMappingListResponse(int Count, List<TopicTemplateMappingDto> Mappings);

public record EnumValueDto(string Name, int Value);

public record EnumValuesResponse(string EnumName, List<EnumValueDto> Values);
