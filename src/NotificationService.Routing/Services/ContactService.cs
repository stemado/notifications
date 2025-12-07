using Microsoft.Extensions.Logging;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service implementation for contact management
/// </summary>
public class ContactService : IContactService
{
    private readonly IContactRepository _repository;
    private readonly ILogger<ContactService> _logger;

    public ContactService(IContactRepository repository, ILogger<ContactService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Contact> CreateAsync(Contact contact)
    {
        // Check for duplicate email
        var existing = await _repository.GetByEmailAsync(contact.Email);
        if (existing != null)
        {
            throw new InvalidOperationException($"Contact with email '{contact.Email}' already exists");
        }

        var created = await _repository.CreateAsync(contact);
        _logger.LogInformation("Created contact {ContactId} ({Email})", created.Id, created.Email);
        return created;
    }

    public async Task<Contact?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Contact?> GetByEmailAsync(string email)
    {
        return await _repository.GetByEmailAsync(email);
    }

    public async Task<List<Contact>> SearchAsync(string searchTerm, bool includeInactive = false)
    {
        return await _repository.SearchAsync(searchTerm, includeInactive);
    }

    public async Task<List<Contact>> GetAllAsync(bool includeInactive = false)
    {
        return await _repository.GetAllAsync(includeInactive);
    }

    public async Task<Contact> UpdateAsync(Contact contact)
    {
        var updated = await _repository.UpdateAsync(contact);
        _logger.LogInformation("Updated contact {ContactId}", contact.Id);
        return updated;
    }

    public async Task DeactivateAsync(Guid id)
    {
        await _repository.DeactivateAsync(id);
        _logger.LogInformation("Deactivated contact {ContactId}", id);
    }

    public async Task<List<RecipientGroup>> GetGroupsForContactAsync(Guid contactId)
    {
        return await _repository.GetGroupsForContactAsync(contactId);
    }
}
