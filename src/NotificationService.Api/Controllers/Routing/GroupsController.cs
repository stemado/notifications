using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for recipient group management in the routing system
/// </summary>
[ApiController]
[Route("api/routing/groups")]
public class GroupsController : ControllerBase
{
    private readonly IRecipientGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IRecipientGroupService groupService, ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    /// <summary>
    /// List all recipient groups, optionally filtering by client
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RecipientGroupSummary>>> ListGroups(
        [FromQuery] string? clientId = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        List<RecipientGroup> groups;

        if (clientId != null)
        {
            groups = await _groupService.GetByClientAsync(clientId);
        }
        else
        {
            groups = await _groupService.GetAllAsync(includeInactive);
        }

        var policies = new Dictionary<Guid, int>();
        foreach (var group in groups)
        {
            var groupPolicies = await _groupService.GetPoliciesUsingGroupAsync(group.Id);
            policies[group.Id] = groupPolicies.Count;
        }

        var allSummaries = groups.Select(g => new RecipientGroupSummary
        {
            Id = g.Id,
            Name = g.Name,
            ClientId = g.ClientId,
            Description = g.Description,
            IsActive = g.IsActive,
            MemberCount = g.Memberships?.Count ?? 0,
            PolicyCount = policies.GetValueOrDefault(g.Id, 0)
        }).ToList();

        var totalItems = allSummaries.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var paginatedData = allSummaries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PaginatedResponse<RecipientGroupSummary>
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
    /// Get recipient group details including members
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecipientGroupDetails>> GetGroup(Guid id)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound($"Recipient group {id} not found");
        }

        var policies = await _groupService.GetPoliciesUsingGroupAsync(id);

        var details = new RecipientGroupDetails
        {
            Id = group.Id,
            Name = group.Name,
            ClientId = group.ClientId,
            Description = group.Description,
            IsActive = group.IsActive,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Members = group.Memberships?.Select(m => new GroupMemberInfo
            {
                ContactId = m.ContactId,
                Name = m.Contact?.Name ?? "",
                Email = m.Contact?.Email ?? "",
                Organization = m.Contact?.Organization,
                IsActive = m.Contact?.IsActive ?? false,
                AddedAt = m.AddedAt,
                AddedBy = m.AddedBy
            }).ToList() ?? new List<GroupMemberInfo>(),
            Policies = policies.Select(p => new PolicySummaryForGroup
            {
                Id = p.Id,
                Service = p.Service.ToString(),
                Topic = p.Topic.ToString(),
                Channel = p.Channel.ToString(),
                Role = p.Role.ToString(),
                IsEnabled = p.IsEnabled
            }).ToList()
        };

        return Ok(details);
    }

    /// <summary>
    /// Create a new recipient group
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RecipientGroupDetails>> CreateGroup([FromBody] CreateRecipientGroupRequest request)
    {
        var group = new RecipientGroup
        {
            Name = request.Name,
            ClientId = request.ClientId,
            Description = request.Description
        };

        var created = await _groupService.CreateAsync(group);
        _logger.LogInformation("Created recipient group {GroupId} via API", created.Id);

        return CreatedAtAction(nameof(GetGroup), new { id = created.Id }, new RecipientGroupDetails
        {
            Id = created.Id,
            Name = created.Name,
            ClientId = created.ClientId,
            Description = created.Description,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            Members = new List<GroupMemberInfo>(),
            Policies = new List<PolicySummaryForGroup>()
        });
    }

    /// <summary>
    /// Update an existing recipient group
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RecipientGroupDetails>> UpdateGroup(Guid id, [FromBody] UpdateRecipientGroupRequest request)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound($"Recipient group {id} not found");
        }

        group.Name = request.Name;
        group.Description = request.Description;
        group.IsActive = request.IsActive;

        var updated = await _groupService.UpdateAsync(group);
        var policies = await _groupService.GetPoliciesUsingGroupAsync(id);

        return Ok(new RecipientGroupDetails
        {
            Id = updated.Id,
            Name = updated.Name,
            ClientId = updated.ClientId,
            Description = updated.Description,
            IsActive = updated.IsActive,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            Members = updated.Memberships?.Select(m => new GroupMemberInfo
            {
                ContactId = m.ContactId,
                Name = m.Contact?.Name ?? "",
                Email = m.Contact?.Email ?? "",
                Organization = m.Contact?.Organization,
                IsActive = m.Contact?.IsActive ?? false,
                AddedAt = m.AddedAt,
                AddedBy = m.AddedBy
            }).ToList() ?? new List<GroupMemberInfo>(),
            Policies = policies.Select(p => new PolicySummaryForGroup
            {
                Id = p.Id,
                Service = p.Service.ToString(),
                Topic = p.Topic.ToString(),
                Channel = p.Channel.ToString(),
                Role = p.Role.ToString(),
                IsEnabled = p.IsEnabled
            }).ToList()
        });
    }

    /// <summary>
    /// Add a member to a group
    /// </summary>
    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult> AddMember(Guid id, [FromBody] AddGroupMemberRequest request)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound($"Recipient group {id} not found");
        }

        var addedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";
        await _groupService.AddMemberAsync(id, request.ContactId, addedBy);

        _logger.LogInformation("Added contact {ContactId} to group {GroupId} via API", request.ContactId, id);
        return Ok();
    }

    /// <summary>
    /// Remove a member from a group
    /// </summary>
    [HttpDelete("{id:guid}/members/{contactId:guid}")]
    public async Task<ActionResult> RemoveMember(Guid id, Guid contactId)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound($"Recipient group {id} not found");
        }

        await _groupService.RemoveMemberAsync(id, contactId);

        _logger.LogInformation("Removed contact {ContactId} from group {GroupId} via API", contactId, id);
        return NoContent();
    }

    /// <summary>
    /// Get members of a group
    /// </summary>
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<ContactSummary>>> GetMembers(Guid id)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound($"Recipient group {id} not found");
        }

        var members = await _groupService.GetMembersAsync(id);
        var summaries = members.Select(c => new ContactSummary
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            Organization = c.Organization,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            GroupCount = c.Memberships?.Count ?? 0
        }).ToList();

        return Ok(summaries);
    }
}
