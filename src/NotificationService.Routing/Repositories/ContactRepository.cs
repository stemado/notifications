using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for Contact data access
/// </summary>
public class ContactRepository : IContactRepository
{
    private readonly RoutingDbContext _context;

    public ContactRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<Contact> CreateAsync(Contact contact)
    {
        contact.CreatedAt = DateTime.UtcNow;
        contact.UpdatedAt = DateTime.UtcNow;
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();
        return contact;
    }

    public async Task<Contact?> GetByIdAsync(Guid id)
    {
        return await _context.Contacts
            .Include(c => c.Memberships)
                .ThenInclude(m => m.Group)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contact?> GetByEmailAsync(string email)
    {
        return await _context.Contacts
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
    }

    public async Task<List<Contact>> SearchAsync(string searchTerm, bool includeInactive = false)
    {
        var query = _context.Contacts.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var lowerSearch = searchTerm.ToLower();
        return await query
            .Where(c => c.Name.ToLower().Contains(lowerSearch)
                     || c.Email.ToLower().Contains(lowerSearch)
                     || (c.Organization != null && c.Organization.ToLower().Contains(lowerSearch)))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Contact>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Contacts.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Contact>> GetByOrganizationAsync(string organization)
    {
        return await _context.Contacts
            .Where(c => c.Organization == organization && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Contact> UpdateAsync(Contact contact)
    {
        contact.UpdatedAt = DateTime.UtcNow;
        _context.Contacts.Update(contact);
        await _context.SaveChangesAsync();
        return contact;
    }

    public async Task DeactivateAsync(Guid id)
    {
        var contact = await _context.Contacts.FindAsync(id)
            ?? throw new InvalidOperationException($"Contact {id} not found");

        contact.IsActive = false;
        contact.DeactivatedAt = DateTime.UtcNow;
        contact.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<RecipientGroup>> GetGroupsForContactAsync(Guid contactId)
    {
        return await _context.GroupMemberships
            .Where(m => m.ContactId == contactId)
            .Include(m => m.Group)
            .Select(m => m.Group!)
            .Where(g => g.IsActive)
            .ToListAsync();
    }
}
