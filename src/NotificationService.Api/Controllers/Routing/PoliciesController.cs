using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for routing policy management
/// </summary>
[ApiController]
[Route("api/routing/policies")]
public class PoliciesController : ControllerBase
{
    private readonly IRoutingPolicyService _policyService;
    private readonly IRecipientGroupService _groupService;
    private readonly ILogger<PoliciesController> _logger;

    public PoliciesController(
        IRoutingPolicyService policyService,
        IRecipientGroupService groupService,
        ILogger<PoliciesController> logger)
    {
        _policyService = policyService;
        _groupService = groupService;
        _logger = logger;
    }

    /// <summary>
    /// List all routing policies, optionally filtering by client, service, or topic
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RoutingPolicySummary>>> ListPolicies(
        [FromQuery] string? clientId = null,
        [FromQuery] SourceService? service = null,
        [FromQuery] NotificationTopic? topic = null,
        [FromQuery] bool includeDisabled = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        List<RoutingPolicy> policies;

        if (clientId != null)
        {
            policies = await _policyService.GetByClientAsync(clientId);
        }
        else if (service.HasValue && topic.HasValue)
        {
            policies = await _policyService.GetByServiceAndTopicAsync(service.Value, topic.Value);
        }
        else
        {
            policies = await _policyService.GetAllAsync(includeDisabled);
        }

        var allSummaries = policies.Select(p => new RoutingPolicySummary
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
        }).ToList();

        var totalItems = allSummaries.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var paginatedData = allSummaries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PaginatedResponse<RoutingPolicySummary>
        {
            Data = paginatedData,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNext = page < totalPages,
            HasPrevious = page > 1
        });
    }

    /// <summary>
    /// Get routing policy details
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoutingPolicyDetails>> GetPolicy(Guid id)
    {
        var policy = await _policyService.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound($"Routing policy {id} not found");
        }

        var members = await _groupService.GetMembersAsync(policy.RecipientGroupId);

        var details = new RoutingPolicyDetails
        {
            Id = policy.Id,
            Service = policy.Service.ToString(),
            Topic = policy.Topic.ToString(),
            ClientId = policy.ClientId,
            MinSeverity = policy.MinSeverity?.ToString(),
            Channel = policy.Channel.ToString(),
            RecipientGroupId = policy.RecipientGroupId,
            RecipientGroupName = policy.RecipientGroup?.Name ?? "",
            RecipientCount = members.Count,
            Role = policy.Role.ToString(),
            Priority = policy.Priority,
            IsEnabled = policy.IsEnabled,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt,
            UpdatedBy = policy.UpdatedBy
        };

        return Ok(details);
    }

    /// <summary>
    /// Create a new routing policy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RoutingPolicyDetails>> CreatePolicy([FromBody] CreateRoutingPolicyRequest request)
    {
        var policy = new RoutingPolicy
        {
            Service = request.Service,
            Topic = request.Topic,
            ClientId = request.ClientId,
            MinSeverity = request.MinSeverity,
            Channel = request.Channel,
            RecipientGroupId = request.RecipientGroupId,
            Role = request.Role,
            Priority = request.Priority,
            UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api"
        };

        var created = await _policyService.CreateAsync(policy);
        var group = await _groupService.GetByIdAsync(created.RecipientGroupId);
        var members = await _groupService.GetMembersAsync(created.RecipientGroupId);

        _logger.LogInformation("Created routing policy {PolicyId} via API", created.Id);

        return CreatedAtAction(nameof(GetPolicy), new { id = created.Id }, new RoutingPolicyDetails
        {
            Id = created.Id,
            Service = created.Service.ToString(),
            Topic = created.Topic.ToString(),
            ClientId = created.ClientId,
            MinSeverity = created.MinSeverity?.ToString(),
            Channel = created.Channel.ToString(),
            RecipientGroupId = created.RecipientGroupId,
            RecipientGroupName = group?.Name ?? "",
            RecipientCount = members.Count,
            Role = created.Role.ToString(),
            Priority = created.Priority,
            IsEnabled = created.IsEnabled,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            UpdatedBy = created.UpdatedBy
        });
    }

    /// <summary>
    /// Update an existing routing policy
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoutingPolicyDetails>> UpdatePolicy(Guid id, [FromBody] UpdateRoutingPolicyRequest request)
    {
        var policy = await _policyService.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound($"Routing policy {id} not found");
        }

        policy.MinSeverity = request.MinSeverity;
        policy.Channel = request.Channel;
        policy.RecipientGroupId = request.RecipientGroupId;
        policy.Role = request.Role;
        policy.Priority = request.Priority;
        policy.IsEnabled = request.IsEnabled;
        policy.UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        var updated = await _policyService.UpdateAsync(policy);
        var group = await _groupService.GetByIdAsync(updated.RecipientGroupId);
        var members = await _groupService.GetMembersAsync(updated.RecipientGroupId);

        return Ok(new RoutingPolicyDetails
        {
            Id = updated.Id,
            Service = updated.Service.ToString(),
            Topic = updated.Topic.ToString(),
            ClientId = updated.ClientId,
            MinSeverity = updated.MinSeverity?.ToString(),
            Channel = updated.Channel.ToString(),
            RecipientGroupId = updated.RecipientGroupId,
            RecipientGroupName = group?.Name ?? "",
            RecipientCount = members.Count,
            Role = updated.Role.ToString(),
            Priority = updated.Priority,
            IsEnabled = updated.IsEnabled,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            UpdatedBy = updated.UpdatedBy
        });
    }

    /// <summary>
    /// Delete a routing policy
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePolicy(Guid id)
    {
        var policy = await _policyService.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound($"Routing policy {id} not found");
        }

        await _policyService.DeleteAsync(id);
        _logger.LogInformation("Deleted routing policy {PolicyId} via API", id);

        return NoContent();
    }

    /// <summary>
    /// Toggle a routing policy enabled/disabled
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    public async Task<ActionResult<RoutingPolicySummary>> TogglePolicy(Guid id)
    {
        var policy = await _policyService.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound($"Routing policy {id} not found");
        }

        var toggled = await _policyService.ToggleAsync(id);
        _logger.LogInformation("Toggled routing policy {PolicyId} to {IsEnabled} via API", id, toggled.IsEnabled);

        return Ok(new RoutingPolicySummary
        {
            Id = toggled.Id,
            Service = toggled.Service.ToString(),
            Topic = toggled.Topic.ToString(),
            ClientId = toggled.ClientId,
            MinSeverity = toggled.MinSeverity?.ToString(),
            Channel = toggled.Channel.ToString(),
            RecipientGroupId = toggled.RecipientGroupId,
            RecipientGroupName = toggled.RecipientGroup?.Name ?? "",
            Role = toggled.Role.ToString(),
            Priority = toggled.Priority,
            IsEnabled = toggled.IsEnabled
        });
    }
}
