using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository interface for Contact data access
/// </summary>
public interface IContactRepository
{
    Task<Contact> CreateAsync(Contact contact);
    Task<Contact?> GetByIdAsync(Guid id);
    Task<Contact?> GetByEmailAsync(string email);
    Task<List<Contact>> SearchAsync(string searchTerm, bool includeInactive = false);
    Task<List<Contact>> GetAllAsync(bool includeInactive = false);
    Task<List<Contact>> GetByOrganizationAsync(string organization);
    Task<Contact> UpdateAsync(Contact contact);
    Task DeactivateAsync(Guid id);
    Task<List<RecipientGroup>> GetGroupsForContactAsync(Guid contactId);
}
