using Microsoft.Extensions.Logging;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service implementation for recipient group management
/// </summary>
public class RecipientGroupService : IRecipientGroupService
{
    private readonly IRecipientGroupRepository _repository;
    private readonly ILogger<RecipientGroupService> _logger;

    public RecipientGroupService(IRecipientGroupRepository repository, ILogger<RecipientGroupService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RecipientGroup> CreateAsync(RecipientGroup group)
    {
        // Check for duplicate name within client scope
        var existing = await _repository.GetByNameAndClientAsync(group.Name, group.ClientId);
        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Recipient group '{group.Name}' already exists for client '{group.ClientId ?? "(global)"}'");
        }

        var created = await _repository.CreateAsync(group);
        _logger.LogInformation("Created recipient group {GroupId} ({Name}) for client {ClientId}",
            created.Id, created.Name, created.ClientId ?? "(global)");
        return created;
    }

    public async Task<RecipientGroup?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<RecipientGroup>> GetByClientAsync(string? clientId)
    {
        return await _repository.GetByClientAsync(clientId);
    }

    public async Task<List<RecipientGroup>> GetAllAsync(bool includeInactive = false)
    {
        return await _repository.GetAllAsync(includeInactive);
    }

    public async Task<RecipientGroup> UpdateAsync(RecipientGroup group)
    {
        var updated = await _repository.UpdateAsync(group);
        _logger.LogInformation("Updated recipient group {GroupId}", group.Id);
        return updated;
    }

    public async Task<List<Contact>> GetMembersAsync(Guid groupId)
    {
        return await _repository.GetMembersAsync(groupId);
    }

    public async Task AddMemberAsync(Guid groupId, Guid contactId, string? addedBy = null)
    {
        await _repository.AddMemberAsync(groupId, contactId, addedBy);
        _logger.LogInformation("Added contact {ContactId} to group {GroupId} by {AddedBy}",
            contactId, groupId, addedBy ?? "system");
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid contactId)
    {
        await _repository.RemoveMemberAsync(groupId, contactId);
        _logger.LogInformation("Removed contact {ContactId} from group {GroupId}", contactId, groupId);
    }

    public async Task<List<RoutingPolicy>> GetPoliciesUsingGroupAsync(Guid groupId)
    {
        return await _repository.GetPoliciesUsingGroupAsync(groupId);
    }
}
