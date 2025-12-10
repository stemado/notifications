using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for outbound routing operations
/// </summary>
[ApiController]
[Route("api/routing")]
public class RoutingController : ControllerBase
{
    private readonly IOutboundRouter _router;
    private readonly IRecipientGroupService _groupService;
    private readonly IRoutingPolicyService _policyService;
    private readonly IContactService _contactService;
    private readonly IRoutingDashboardService _dashboardService;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(
        IOutboundRouter router,
        IRecipientGroupService groupService,
        IRoutingPolicyService policyService,
        IContactService contactService,
        IRoutingDashboardService dashboardService,
        ILogger<RoutingController> logger)
    {
        _router = router;
        _groupService = groupService;
        _policyService = policyService;
        _contactService = contactService;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get the routing dashboard with statistics, recent events, and failed deliveries
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<RoutingDashboardData>> GetDashboard()
    {
        var dashboardData = await _dashboardService.GetDashboardDataAsync();
        return Ok(dashboardData);
    }

    /// <summary>
    /// Publish an outbound event to be routed to recipients
    /// </summary>
    [HttpPost("publish")]
    public async Task<ActionResult<OutboundEventSummary>> PublishEvent([FromBody] PublishEventRequest request)
    {
        var evt = new OutboundEvent
        {
            Service = request.Service,
            Topic = request.Topic,
            ClientId = request.ClientId,
            Severity = request.Severity,
            TemplateId = request.TemplateId,
            Subject = request.Subject,
            Body = request.Body,
            Payload = request.Payload ?? new Dictionary<string, object>(),
            SagaId = request.SagaId,
            CorrelationId = request.CorrelationId
        };

        var eventId = await _router.PublishAsync(evt);
        var createdEvent = await _router.GetEventAsync(eventId);

        _logger.LogInformation("Published outbound event {EventId} via API", eventId);

        return Ok(MapToEventSummary(createdEvent!));
    }

    /// <summary>
    /// Get an outbound event by ID
    /// </summary>
    [HttpGet("events/{id:guid}")]
    public async Task<ActionResult<OutboundEventDetails>> GetEvent(Guid id)
    {
        var evt = await _router.GetEventAsync(id);
        if (evt == null)
        {
            return NotFound($"Event {id} not found");
        }

        return Ok(MapToEventDetails(evt));
    }

    /// <summary>
    /// Get events for a specific saga
    /// </summary>
    [HttpGet("events/saga/{sagaId:guid}")]
    public async Task<ActionResult<List<OutboundEventSummary>>> GetEventsBySaga(Guid sagaId)
    {
        var events = await _router.GetEventsBySagaAsync(sagaId);
        return Ok(events.Select(MapToEventSummary).ToList());
    }

    /// <summary>
    /// Get the complete routing configuration for a client
    /// </summary>
    [HttpGet("clients/{clientId}/configuration")]
    public async Task<ActionResult<ClientRoutingConfiguration>> GetClientConfiguration(string clientId)
    {
        // Get client-specific and global groups
        var clientGroups = await _groupService.GetByClientAsync(clientId);
        var globalGroups = await _groupService.GetByClientAsync(null);
        var allGroups = clientGroups.Concat(globalGroups).ToList();

        // Get client-specific and default policies
        var clientPolicies = await _policyService.GetByClientAsync(clientId);
        var defaultPolicies = await _policyService.GetByClientAsync(null);
        var allPolicies = clientPolicies.Concat(defaultPolicies).ToList();

        // Get unique contacts across all groups
        var contactIds = new HashSet<Guid>();
        foreach (var group in allGroups)
        {
            var members = await _groupService.GetMembersAsync(group.Id);
            foreach (var member in members)
            {
                contactIds.Add(member.Id);
            }
        }

        var contacts = new List<ContactSummary>();
        foreach (var contactId in contactIds)
        {
            var contact = await _contactService.GetByIdAsync(contactId);
            if (contact != null && contact.IsActive)
            {
                contacts.Add(new ContactSummary
                {
                    Id = contact.Id,
                    Name = contact.Name,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Organization = contact.Organization,
                    IsActive = contact.IsActive,
                    CreatedAt = contact.CreatedAt,
                    GroupCount = contact.Memberships?.Count ?? 0
                });
            }
        }

        var config = new ClientRoutingConfiguration
        {
            ClientId = clientId,
            Groups = allGroups.Select(g => new RecipientGroupSummary
            {
                Id = g.Id,
                Name = g.Name,
                ClientId = g.ClientId,
                Description = g.Description,
                IsActive = g.IsActive,
                MemberCount = g.Memberships?.Count ?? 0,
                PolicyCount = g.Policies?.Count ?? 0
            }).ToList(),
            Policies = allPolicies.Select(p => new RoutingPolicySummary
            {
                Id = p.Id,
                Service = p.Service.ToString(),
                Topic = p.Topic.ToString(),
                ClientId = p.ClientId,
                MinSeverity = p.MinSeverity?.ToString(),
                Channel = p.Channel.ToString(),
                RecipientGroupId = p.RecipientGroupId,
                RecipientGroupName = p.RecipientGroup?.Name ?? "",
                Role = p.Role.ToString(),
                Priority = p.Priority,
                IsEnabled = p.IsEnabled
            }).ToList(),
            Contacts = contacts,
            Stats = new ClientRoutingStats
            {
                TotalGroups = allGroups.Count,
                TotalPolicies = allPolicies.Count,
                ActivePolicies = allPolicies.Count(p => p.IsEnabled),
                UniqueContacts = contacts.Count,
                PolicyCoverageByTopic = allPolicies.Select(p => p.Topic).Distinct().Count()
            }
        };

        return Ok(config);
    }

    private static OutboundEventSummary MapToEventSummary(OutboundEvent evt)
    {
        return new OutboundEventSummary
        {
            Id = evt.Id,
            Service = evt.Service.ToString(),
            Topic = evt.Topic.ToString(),
            ClientId = evt.ClientId,
            Severity = evt.Severity.ToString(),
            Subject = evt.Subject,
            SagaId = evt.SagaId,
            CreatedAt = evt.CreatedAt,
            ProcessedAt = evt.ProcessedAt,
            DeliveryCount = evt.Deliveries?.Count ?? 0,
            PendingCount = evt.Deliveries?.Count(d => d.Status == NotificationService.Domain.Enums.DeliveryStatus.Pending) ?? 0,
            DeliveredCount = evt.Deliveries?.Count(d => d.Status == NotificationService.Domain.Enums.DeliveryStatus.Delivered) ?? 0,
            FailedCount = evt.Deliveries?.Count(d => d.Status == NotificationService.Domain.Enums.DeliveryStatus.Failed) ?? 0
        };
    }

    private static OutboundEventDetails MapToEventDetails(OutboundEvent evt)
    {
        return new OutboundEventDetails
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
        };
    }
}
