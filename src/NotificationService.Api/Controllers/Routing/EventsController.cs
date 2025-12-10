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
