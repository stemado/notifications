using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Data;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for outbound event management
/// </summary>
[ApiController]
[Route("api/routing/events")]
public class EventsController : ControllerBase
{
    private readonly RoutingDbContext _dbContext;
    private readonly IOutboundRouter _router;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        RoutingDbContext dbContext,
        IOutboundRouter router,
        ILogger<EventsController> logger)
    {
        _dbContext = dbContext;
        _router = router;
        _logger = logger;
    }

    /// <summary>
    /// List outbound events with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<OutboundEventSummary>>> ListEvents(
        [FromQuery] string? clientId = null,
        [FromQuery] string? service = null,
        [FromQuery] string? topic = null,
        [FromQuery] Guid? sagaId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.OutboundEvents
            .Include(e => e.Deliveries)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(clientId))
            query = query.Where(e => e.ClientId == clientId);

        if (!string.IsNullOrEmpty(service) && Enum.TryParse<NotificationService.Routing.Domain.Enums.SourceService>(service, out var svc))
            query = query.Where(e => e.Service == svc);

        if (!string.IsNullOrEmpty(topic) && Enum.TryParse<NotificationService.Routing.Domain.Enums.NotificationTopic>(topic, out var tp))
            query = query.Where(e => e.Topic == tp);

        if (sagaId.HasValue)
            query = query.Where(e => e.SagaId == sagaId.Value);

        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAt <= toDate.Value);

        // Get total count
        var totalItems = await query.CountAsync();

        // Apply pagination
        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var summaries = events.Select(e => new OutboundEventSummary
        {
            Id = e.Id,
            Service = e.Service.ToString(),
            Topic = e.Topic.ToString(),
            ClientId = e.ClientId,
            Severity = e.Severity.ToString(),
            Subject = e.Subject,
            SagaId = e.SagaId,
            CreatedAt = e.CreatedAt,
            ProcessedAt = e.ProcessedAt,
            DeliveryCount = e.Deliveries?.Count ?? 0,
            PendingCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Pending) ?? 0,
            DeliveredCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Delivered) ?? 0,
            FailedCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Failed) ?? 0
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return Ok(new PaginatedResponse<OutboundEventSummary>
        {
            Data = summaries,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNext = page < totalPages,
            HasPrevious = page > 1
        });
    }

    /// <summary>
    /// Get an outbound event by ID with delivery details
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OutboundEventDetails>> GetEvent(Guid id)
    {
        var evt = await _router.GetEventAsync(id);
        if (evt == null)
        {
            return NotFound($"Event {id} not found");
        }

        return Ok(new OutboundEventDetails
        {
            Id = evt.Id,
            Service = evt.Service.ToString(),
            Topic = evt.Topic.ToString(),
            ClientId = evt.ClientId,
            Severity = evt.Severity.ToString(),
            TemplateId = evt.TemplateId,
            Subject = evt.Subject,
            Body = evt.Body,
            Payload = evt.Payload,
            SagaId = evt.SagaId,
            CorrelationId = evt.CorrelationId,
            CreatedAt = evt.CreatedAt,
            ProcessedAt = evt.ProcessedAt,
            Deliveries = evt.Deliveries?.Select(d => new DeliveryInfo
            {
                Id = d.Id,
                ContactId = d.ContactId,
                ContactName = d.Contact?.Name ?? "",
                ContactEmail = d.Contact?.Email ?? "",
                Channel = d.Channel.ToString(),
                Role = d.Role.ToString(),
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                SentAt = d.SentAt,
                DeliveredAt = d.DeliveredAt,
                FailedAt = d.FailedAt,
                ErrorMessage = d.ErrorMessage,
                AttemptCount = d.AttemptCount
            }).ToList() ?? new List<DeliveryInfo>()
        });
    }

    /// <summary>
    /// Get events for a specific saga
    /// </summary>
    [HttpGet("saga/{sagaId:guid}")]
    public async Task<ActionResult<List<OutboundEventSummary>>> GetEventsBySaga(Guid sagaId)
    {
        var events = await _router.GetEventsBySagaAsync(sagaId);
        return Ok(events.Select(e => new OutboundEventSummary
        {
            Id = e.Id,
            Service = e.Service.ToString(),
            Topic = e.Topic.ToString(),
            ClientId = e.ClientId,
            Severity = e.Severity.ToString(),
            Subject = e.Subject,
            SagaId = e.SagaId,
            CreatedAt = e.CreatedAt,
            ProcessedAt = e.ProcessedAt,
            DeliveryCount = e.Deliveries?.Count ?? 0,
            PendingCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Pending) ?? 0,
            DeliveredCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Delivered) ?? 0,
            FailedCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Failed) ?? 0
        }).ToList());
    }

    /// <summary>
    /// Publish an outbound event for routing to recipients.
    /// The routing engine evaluates policies and creates deliveries for matching recipients.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PublishEventResponse>> PublishEvent([FromBody] PublishEventApiRequest request)
    {
        // Parse service enum
        if (!Enum.TryParse<NotificationService.Routing.Domain.Enums.SourceService>(request.Service, true, out var service))
        {
            return BadRequest($"Invalid service: {request.Service}. Valid values: {string.Join(", ", Enum.GetNames<NotificationService.Routing.Domain.Enums.SourceService>())}");
        }

        // Parse topic enum
        if (!Enum.TryParse<NotificationService.Routing.Domain.Enums.NotificationTopic>(request.Topic, true, out var topic))
        {
            return BadRequest($"Invalid topic: {request.Topic}. Valid values: {string.Join(", ", Enum.GetNames<NotificationService.Routing.Domain.Enums.NotificationTopic>())}");
        }

        // Parse severity enum (default to Info)
        var severity = NotificationSeverity.Info;
        if (!string.IsNullOrEmpty(request.Severity) &&
            !Enum.TryParse<NotificationSeverity>(request.Severity, true, out severity))
        {
            return BadRequest($"Invalid severity: {request.Severity}. Valid values: {string.Join(", ", Enum.GetNames<NotificationSeverity>())}");
        }

        _logger.LogInformation(
            "Publishing outbound event: {Service}/{Topic} for client {ClientId}, saga {SagaId}",
            service, topic, request.ClientId, request.SagaId);

        // Create the outbound event
        var outboundEvent = new NotificationService.Routing.Domain.Models.OutboundEvent
        {
            Service = service,
            Topic = topic,
            ClientId = request.ClientId,
            Severity = severity,
            TemplateId = request.TemplateId,
            Subject = request.Subject,
            Body = request.Body,
            SagaId = request.SagaId,
            CorrelationId = request.CorrelationId,
            CreatedAt = DateTime.UtcNow
        };

        // Convert payload if provided
        if (request.Payload != null)
        {
            foreach (var kvp in request.Payload)
            {
                outboundEvent.Payload[kvp.Key] = System.Text.Json.JsonSerializer.SerializeToElement(kvp.Value);
            }
        }

        try
        {
            var eventId = await _router.PublishAsync(outboundEvent);

            // Get the event to count deliveries
            var createdEvent = await _router.GetEventAsync(eventId);
            var deliveryCount = createdEvent?.Deliveries?.Count ?? 0;

            return Ok(new PublishEventResponse
            {
                EventId = eventId,
                DeliveryCount = deliveryCount,
                HasMatchingPolicies = deliveryCount > 0,
                Message = deliveryCount > 0
                    ? $"Event published successfully. {deliveryCount} deliveries queued."
                    : "Event published but no matching routing policies found. No deliveries created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish outbound event");
            return StatusCode(500, new { error = "Failed to publish event", message = ex.Message });
        }
    }
}

/// <summary>
/// API request for publishing an event (uses strings for enums for easier integration)
/// </summary>
public record PublishEventApiRequest
{
    /// <summary>
    /// Source service (e.g., "CensusReconciliation", "CensusOrchestration")
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// Notification topic (e.g., "DailyImportSuccess", "ReconciliationEscalation")
    /// </summary>
    public required string Topic { get; init; }

    /// <summary>
    /// Client ID this event is associated with
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Severity level (Info, Warning, Error, Critical). Defaults to Info.
    /// </summary>
    public string? Severity { get; init; }

    /// <summary>
    /// Optional template ID for rendering
    /// </summary>
    public string? TemplateId { get; init; }

    /// <summary>
    /// Subject line for the notification
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Body content (can be HTML for email)
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Additional payload data for template rendering
    /// </summary>
    public Dictionary<string, object>? Payload { get; init; }

    /// <summary>
    /// Associated saga ID for correlation
    /// </summary>
    public Guid? SagaId { get; init; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public Guid? CorrelationId { get; init; }
}

/// <summary>
/// Generic paginated response
/// </summary>
public record PaginatedResponse<T>
{
    public List<T> Data { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasNext { get; init; }
    public bool HasPrevious { get; init; }
}
