using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for contact management in the routing system
/// </summary>
[ApiController]
[Route("api/routing/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(IContactService contactService, ILogger<ContactsController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    /// <summary>
    /// List all contacts, optionally filtering by search term
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContactSummary>>> ListContacts(
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false)
    {
        var contacts = string.IsNullOrWhiteSpace(search)
            ? await _contactService.GetAllAsync(includeInactive)
            : await _contactService.SearchAsync(search, includeInactive);

        var summaries = contacts.Select(c => new ContactSummary
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

    /// <summary>
    /// Get contact details including group memberships
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDetails>> GetContact(Guid id)
    {
        var contact = await _contactService.GetByIdAsync(id);
        if (contact == null)
        {
            return NotFound($"Contact {id} not found");
        }

        var details = new ContactDetails
        {
            Id = contact.Id,
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone,
            Organization = contact.Organization,
            IsActive = contact.IsActive,
            UserId = contact.UserId,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt,
            DeactivatedAt = contact.DeactivatedAt,
            Notes = contact.Notes,
            Groups = contact.Memberships?.Select(m => new GroupMembershipInfo
            {
                GroupId = m.GroupId,
                GroupName = m.Group?.Name ?? "",
                ClientId = m.Group?.ClientId,
                AddedAt = m.AddedAt
            }).ToList() ?? new List<GroupMembershipInfo>()
        };

        return Ok(details);
    }

    /// <summary>
    /// Create a new contact
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContactDetails>> CreateContact([FromBody] CreateContactRequest request)
    {
        var contact = new Contact
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Organization = request.Organization,
            Notes = request.Notes
        };

        var created = await _contactService.CreateAsync(contact);
        _logger.LogInformation("Created contact {ContactId} via API", created.Id);

        return CreatedAtAction(nameof(GetContact), new { id = created.Id }, new ContactDetails
        {
            Id = created.Id,
            Name = created.Name,
            Email = created.Email,
            Phone = created.Phone,
            Organization = created.Organization,
            IsActive = created.IsActive,
            UserId = created.UserId,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            Notes = created.Notes,
            Groups = new List<GroupMembershipInfo>()
        });
    }

    /// <summary>
    /// Update an existing contact
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactDetails>> UpdateContact(Guid id, [FromBody] UpdateContactRequest request)
    {
        var contact = await _contactService.GetByIdAsync(id);
        if (contact == null)
        {
            return NotFound($"Contact {id} not found");
        }

        contact.Name = request.Name;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.Organization = request.Organization;
        contact.Notes = request.Notes;

        var updated = await _contactService.UpdateAsync(contact);

        return Ok(new ContactDetails
        {
            Id = updated.Id,
            Name = updated.Name,
            Email = updated.Email,
            Phone = updated.Phone,
            Organization = updated.Organization,
            IsActive = updated.IsActive,
            UserId = updated.UserId,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            DeactivatedAt = updated.DeactivatedAt,
            Notes = updated.Notes,
            Groups = updated.Memberships?.Select(m => new GroupMembershipInfo
            {
                GroupId = m.GroupId,
                GroupName = m.Group?.Name ?? "",
                ClientId = m.Group?.ClientId,
                AddedAt = m.AddedAt
            }).ToList() ?? new List<GroupMembershipInfo>()
        });
    }

    /// <summary>
    /// Deactivate a contact (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeactivateContact(Guid id)
    {
        var contact = await _contactService.GetByIdAsync(id);
        if (contact == null)
        {
            return NotFound($"Contact {id} not found");
        }

        await _contactService.DeactivateAsync(id);
        _logger.LogInformation("Deactivated contact {ContactId} via API", id);

        return NoContent();
    }

    /// <summary>
    /// Get groups for a specific contact
    /// </summary>
    [HttpGet("{id:guid}/groups")]
    public async Task<ActionResult<List<RecipientGroupSummary>>> GetContactGroups(Guid id)
    {
        var contact = await _contactService.GetByIdAsync(id);
        if (contact == null)
        {
            return NotFound($"Contact {id} not found");
        }

        var groups = await _contactService.GetGroupsForContactAsync(id);
        var summaries = groups.Select(g => new RecipientGroupSummary
        {
            Id = g.Id,
            Name = g.Name,
            ClientId = g.ClientId,
            Description = g.Description,
            IsActive = g.IsActive,
            MemberCount = g.Memberships?.Count ?? 0,
            PolicyCount = g.Policies?.Count ?? 0
        }).ToList();

        return Ok(summaries);
    }
}
