using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service interface for recipient group management
/// </summary>
public interface IRecipientGroupService
{
    Task<RecipientGroup> CreateAsync(RecipientGroup group);
    Task<RecipientGroup?> GetByIdAsync(Guid id);
    Task<List<RecipientGroup>> GetByClientAsync(string? clientId);
    Task<List<RecipientGroup>> GetAllAsync(bool includeInactive = false);
    Task<RecipientGroup> UpdateAsync(RecipientGroup group);
    Task<List<Contact>> GetMembersAsync(Guid groupId);
    Task AddMemberAsync(Guid groupId, Guid contactId, string? addedBy = null);
    Task RemoveMemberAsync(Guid groupId, Guid contactId);
    Task<List<RoutingPolicy>> GetPoliciesUsingGroupAsync(Guid groupId);
}
