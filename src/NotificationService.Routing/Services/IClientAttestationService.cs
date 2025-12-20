using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// DTO for attestation template with its client-specific configuration
/// </summary>
public record AttestationTemplateDto(
    int TemplateId,
    string Name,
    string? Description,
    string TemplateType,
    bool IsActive
);

/// <summary>
/// DTO for assigned policy information
/// </summary>
public record AssignedPolicyDto(
    Guid Id,
    string Name,
    string Topic,
    string Service,
    string? GroupName
);

/// <summary>
/// DTO for assigned group information
/// </summary>
public record AssignedGroupDto(
    Guid Id,
    string Name,
    string Role,
    int MemberCount
);

/// <summary>
/// DTO for a client's template configuration including policies and groups
/// </summary>
public record ClientAttestationTemplateDto(
    Guid Id,
    int TemplateId,
    string TemplateName,
    string? TemplateDescription,
    bool IsEnabled,
    int Priority,
    string? Notes,
    List<AssignedPolicyDto> AssignedPolicies,
    List<AssignedGroupDto> AssignedGroups
);

/// <summary>
/// DTO for complete client attestation configuration
/// </summary>
public record ClientAttestationConfigurationDto(
    string ClientId,
    List<ClientAttestationTemplateDto> Templates,
    ClientAttestationStatsDto Stats
);

/// <summary>
/// DTO for attestation configuration statistics
/// </summary>
public record ClientAttestationStatsDto(
    int TotalTemplates,
    int EnabledTemplates,
    int TotalPolicies,
    int TotalGroups
);

/// <summary>
/// Request DTO for enabling/updating a template for a client
/// </summary>
public record EnableTemplateRequest(
    bool IsEnabled,
    int Priority = 0,
    string? Notes = null
);

/// <summary>
/// Service interface for client attestation template management
/// </summary>
public interface IClientAttestationService
{
    /// <summary>
    /// Get all attestation-eligible templates (templates that can be assigned to clients)
    /// </summary>
    Task<List<AttestationTemplateDto>> GetAttestationTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get complete attestation configuration for a client, including all templates with their current state
    /// </summary>
    Task<ClientAttestationConfigurationDto> GetClientConfigurationAsync(string clientId, CancellationToken ct = default);

    /// <summary>
    /// Enable or update a template for a client
    /// </summary>
    Task<ClientAttestationTemplateDto> EnableTemplateAsync(
        string clientId,
        int templateId,
        EnableTemplateRequest request,
        string? updatedBy = null,
        CancellationToken ct = default);

    /// <summary>
    /// Disable/remove a template from a client
    /// </summary>
    Task DisableTemplateAsync(string clientId, int templateId, CancellationToken ct = default);

    /// <summary>
    /// Toggle a template's enabled state for a client
    /// </summary>
    Task<ClientAttestationTemplateDto> ToggleTemplateAsync(
        string clientId,
        int templateId,
        string? updatedBy = null,
        CancellationToken ct = default);

    /// <summary>
    /// Add a routing policy to a client's template
    /// </summary>
    Task<AssignedPolicyDto> AddPolicyAsync(
        string clientId,
        int templateId,
        Guid policyId,
        string? createdBy = null,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a routing policy from a client's template
    /// </summary>
    Task RemovePolicyAsync(string clientId, int templateId, Guid policyId, CancellationToken ct = default);

    /// <summary>
    /// Add a recipient group to a client's template
    /// </summary>
    Task<AssignedGroupDto> AddGroupAsync(
        string clientId,
        int templateId,
        Guid groupId,
        string role,
        string? createdBy = null,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a recipient group from a client's template
    /// </summary>
    Task RemoveGroupAsync(string clientId, int templateId, Guid groupId, CancellationToken ct = default);
}
