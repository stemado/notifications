using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository interface for ClientAttestationTemplate data access
/// </summary>
public interface IClientAttestationRepository
{
    // Client Attestation Template operations
    Task<ClientAttestationTemplate> CreateAsync(ClientAttestationTemplate template);
    Task<ClientAttestationTemplate?> GetByIdAsync(Guid id);
    Task<ClientAttestationTemplate?> GetByClientAndTemplateAsync(string clientId, int templateId);
    Task<List<ClientAttestationTemplate>> GetByClientAsync(string clientId, bool includeDisabled = false);
    Task<ClientAttestationTemplate> UpdateAsync(ClientAttestationTemplate template);
    Task DeleteAsync(Guid id);
    Task<ClientAttestationTemplate> ToggleAsync(Guid id);

    // Policy assignment operations
    Task<ClientAttestationTemplatePolicy> AddPolicyAsync(Guid clientAttestationTemplateId, Guid routingPolicyId, string? createdBy = null);
    Task RemovePolicyAsync(Guid clientAttestationTemplateId, Guid routingPolicyId);
    Task<List<ClientAttestationTemplatePolicy>> GetPoliciesAsync(Guid clientAttestationTemplateId);

    // Group assignment operations
    Task<ClientAttestationTemplateGroup> AddGroupAsync(Guid clientAttestationTemplateId, Guid recipientGroupId, string role, string? createdBy = null);
    Task RemoveGroupAsync(Guid clientAttestationTemplateId, Guid recipientGroupId);
    Task<List<ClientAttestationTemplateGroup>> GetGroupsAsync(Guid clientAttestationTemplateId);
}
