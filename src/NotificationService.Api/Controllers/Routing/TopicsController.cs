using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for topic registry management.
/// Topics provide metadata for notification flow visualization.
/// </summary>
[ApiController]
[Route("api/routing/topics")]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;
    private readonly ILogger<TopicsController> _logger;

    public TopicsController(
        ITopicService topicService,
        ILogger<TopicsController> logger)
    {
        _topicService = topicService;
        _logger = logger;
    }

    /// <summary>
    /// List all topics, optionally filtering by service
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TopicSummary>>> ListTopics(
        [FromQuery] SourceService? service = null,
        [FromQuery] bool includeInactive = false)
    {
        List<Topic> topics;

        if (service.HasValue)
        {
            topics = await _topicService.GetByServiceAsync(service.Value);
        }
        else
        {
            topics = await _topicService.GetAllAsync(includeInactive);
        }

        var summaries = topics.Select(t => new TopicSummary
        {
            Id = t.Id,
            Service = t.Service.ToString(),
            TopicName = t.TopicName.ToString(),
            DisplayName = t.DisplayName,
            Description = t.Description,
            IsActive = t.IsActive
        }).ToList();

        return Ok(summaries);
    }

    /// <summary>
    /// Get topic details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TopicDetails>> GetTopic(Guid id)
    {
        var topic = await _topicService.GetByIdAsync(id);
        if (topic == null)
        {
            return NotFound($"Topic {id} not found");
        }

        return Ok(MapToDetails(topic));
    }

    /// <summary>
    /// Get topic by service and topic name
    /// </summary>
    [HttpGet("{service}/{topicName}")]
    public async Task<ActionResult<TopicDetails>> GetTopicByServiceAndName(
        SourceService service,
        NotificationTopic topicName)
    {
        var topic = await _topicService.GetByServiceAndTopicAsync(service, topicName);
        if (topic == null)
        {
            return NotFound($"Topic {service}/{topicName} not found");
        }

        return Ok(MapToDetails(topic));
    }

    /// <summary>
    /// Create a new topic registry entry
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TopicDetails>> CreateTopic([FromBody] CreateTopicRequest request)
    {
        // Check if topic already exists
        var existing = await _topicService.GetByServiceAndTopicAsync(request.Service, request.TopicName);
        if (existing != null)
        {
            return Conflict($"Topic {request.Service}/{request.TopicName} already exists");
        }

        var topic = new Topic
        {
            Service = request.Service,
            TopicName = request.TopicName,
            DisplayName = request.DisplayName,
            Description = request.Description,
            TriggerDescription = request.TriggerDescription,
            PayloadSchema = request.PayloadSchema,
            DocsUrl = request.DocsUrl,
            UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api"
        };

        var created = await _topicService.CreateAsync(topic);
        _logger.LogInformation("Created topic {Service}/{TopicName} via API", created.Service, created.TopicName);

        return CreatedAtAction(nameof(GetTopic), new { id = created.Id }, MapToDetails(created));
    }

    /// <summary>
    /// Update an existing topic
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TopicDetails>> UpdateTopic(Guid id, [FromBody] UpdateTopicRequest request)
    {
        var topic = await _topicService.GetByIdAsync(id);
        if (topic == null)
        {
            return NotFound($"Topic {id} not found");
        }

        topic.DisplayName = request.DisplayName;
        topic.Description = request.Description;
        topic.TriggerDescription = request.TriggerDescription;
        topic.PayloadSchema = request.PayloadSchema;
        topic.DocsUrl = request.DocsUrl;
        topic.IsActive = request.IsActive;
        topic.UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        var updated = await _topicService.UpdateAsync(topic);
        _logger.LogInformation("Updated topic {TopicId} via API", id);

        return Ok(MapToDetails(updated));
    }

    /// <summary>
    /// Delete a topic
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteTopic(Guid id)
    {
        var topic = await _topicService.GetByIdAsync(id);
        if (topic == null)
        {
            return NotFound($"Topic {id} not found");
        }

        await _topicService.DeleteAsync(id);
        _logger.LogInformation("Deleted topic {TopicId} via API", id);

        return NoContent();
    }

    private static TopicDetails MapToDetails(Topic topic)
    {
        return new TopicDetails
        {
            Id = topic.Id,
            Service = topic.Service.ToString(),
            TopicName = topic.TopicName.ToString(),
            DisplayName = topic.DisplayName,
            Description = topic.Description,
            TriggerDescription = topic.TriggerDescription,
            PayloadSchema = topic.PayloadSchema,
            DocsUrl = topic.DocsUrl,
            IsActive = topic.IsActive,
            CreatedAt = topic.CreatedAt,
            UpdatedAt = topic.UpdatedAt,
            UpdatedBy = topic.UpdatedBy
        };
    }
}
