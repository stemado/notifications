using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for RecipientGroup data access
/// </summary>
public class RecipientGroupRepository : IRecipientGroupRepository
{
    private readonly RoutingDbContext _context;

    public RecipientGroupRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<RecipientGroup> CreateAsync(RecipientGroup group)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        _context.RecipientGroups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<RecipientGroup?> GetByIdAsync(Guid id)
    {
        return await _context.RecipientGroups
            .Include(g => g.Memberships)
                .ThenInclude(m => m.Contact)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<RecipientGroup?> GetByNameAndClientAsync(string name, string? clientId)
    {
        return await _context.RecipientGroups
            .FirstOrDefaultAsync(g => g.Name == name && g.ClientId == clientId);
    }

    public async Task<List<RecipientGroup>> GetByClientAsync(string? clientId)
    {
        return await _context.RecipientGroups
            .Where(g => g.ClientId == clientId && g.IsActive)
            .Include(g => g.Memberships)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<List<RecipientGroup>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.RecipientGroups.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(g => g.IsActive);
        }

        return await query
            .Include(g => g.Memberships)
            .OrderBy(g => g.ClientId)
            .ThenBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<RecipientGroup> UpdateAsync(RecipientGroup group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        _context.RecipientGroups.Update(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<List<Contact>> GetMembersAsync(Guid groupId)
    {
        return await _context.GroupMemberships
            .Where(m => m.GroupId == groupId)
            .Include(m => m.Contact)
            .Select(m => m.Contact!)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddMemberAsync(Guid groupId, Guid contactId, string? addedBy = null)
    {
        var existing = await _context.GroupMemberships
            .AnyAsync(m => m.GroupId == groupId && m.ContactId == contactId);

        if (existing)
        {
            throw new InvalidOperationException($"Contact {contactId} is already a member of group {groupId}");
        }

        var membership = new GroupMembership
        {
            GroupId = groupId,
            ContactId = contactId,
            AddedAt = DateTime.UtcNow,
            AddedBy = addedBy
        };

        _context.GroupMemberships.Add(membership);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid contactId)
    {
        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.ContactId == contactId)
            ?? throw new InvalidOperationException($"Contact {contactId} is not a member of group {groupId}");

        _context.GroupMemberships.Remove(membership);
        await _context.SaveChangesAsync();
    }

    public async Task<List<RoutingPolicy>> GetPoliciesUsingGroupAsync(Guid groupId)
    {
        return await _context.RoutingPolicies
            .Where(p => p.RecipientGroupId == groupId)
            .ToListAsync();
    }
}
