using Microsoft.Extensions.Logging;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service implementation for client attestation template management
/// </summary>
public class ClientAttestationService : IClientAttestationService
{
    private readonly IClientAttestationRepository _attestationRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IRoutingPolicyRepository _policyRepository;
    private readonly IRecipientGroupRepository _groupRepository;
    private readonly ILogger<ClientAttestationService> _logger;

    // Template types considered "attestation" templates
    // All email template types that can be assigned to clients for attestation/notification purposes
    private static readonly HashSet<string> AttestationTemplateTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Original types
        "attestation",
        "notification",
        "success",
        "summary",
        // Report and alert types
        "report",
        "alert",
        // Workflow event types
        "file_detected",
        "validation_failed",
        "archive_completed",
        "workflow_triggered"
    };

    public ClientAttestationService(
        IClientAttestationRepository attestationRepository,
        IEmailTemplateRepository templateRepository,
        IRoutingPolicyRepository policyRepository,
        IRecipientGroupRepository groupRepository,
        ILogger<ClientAttestationService> logger)
    {
        _attestationRepository = attestationRepository;
        _templateRepository = templateRepository;
        _policyRepository = policyRepository;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task<List<AttestationTemplateDto>> GetAttestationTemplatesAsync(CancellationToken ct = default)
    {
        var templates = await _templateRepository.GetActiveTemplatesAsync(ct);

        // Filter to attestation-eligible templates
        return templates
            .Where(t => AttestationTemplateTypes.Contains(t.TemplateType))
            .Select(t => new AttestationTemplateDto(
                t.Id,
                t.Name,
                t.Description,
                t.TemplateType,
                t.IsActive))
            .ToList();
    }

    public async Task<ClientAttestationConfigurationDto> GetClientConfigurationAsync(
        string clientId,
        CancellationToken ct = default)
    {
        // Get all available attestation templates
        var allTemplates = await GetAttestationTemplatesAsync(ct);

        // Get client's existing configurations
        var clientConfigs = await _attestationRepository.GetByClientAsync(clientId, includeDisabled: true);
        var configsByTemplateId = clientConfigs.ToDictionary(c => c.TemplateId);

        // Build the complete configuration list
        var templateDtos = new List<ClientAttestationTemplateDto>();

        foreach (var template in allTemplates)
        {
            if (configsByTemplateId.TryGetValue(template.TemplateId, out var config))
            {
                // Template has a configuration for this client
                templateDtos.Add(MapToDto(config, template));
            }
            else
            {
                // Template is available but not configured for this client
                templateDtos.Add(new ClientAttestationTemplateDto(
                    Id: Guid.Empty,
                    TemplateId: template.TemplateId,
                    TemplateName: template.Name,
                    TemplateDescription: template.Description,
                    IsEnabled: false,
                    Priority: 0,
                    Notes: null,
                    AssignedPolicies: new List<AssignedPolicyDto>(),
                    AssignedGroups: new List<AssignedGroupDto>()));
            }
        }

        // Calculate stats
        var stats = new ClientAttestationStatsDto(
            TotalTemplates: allTemplates.Count,
            EnabledTemplates: templateDtos.Count(t => t.IsEnabled),
            TotalPolicies: templateDtos.Sum(t => t.AssignedPolicies.Count),
            TotalGroups: templateDtos.Sum(t => t.AssignedGroups.Count));

        return new ClientAttestationConfigurationDto(clientId, templateDtos, stats);
    }

    public async Task<ClientAttestationTemplateDto> EnableTemplateAsync(
        string clientId,
        int templateId,
        EnableTemplateRequest request,
        string? updatedBy = null,
        CancellationToken ct = default)
    {
        // Verify template exists
        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new InvalidOperationException($"Email template {templateId} not found");

        // Check if configuration already exists
        var existing = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId);

        ClientAttestationTemplate config;
        if (existing != null)
        {
            // Update existing configuration
            existing.IsEnabled = request.IsEnabled;
            existing.Priority = request.Priority;
            existing.Notes = request.Notes;
            existing.UpdatedBy = updatedBy;
            config = await _attestationRepository.UpdateAsync(existing);

            _logger.LogInformation(
                "Updated attestation template config for client {ClientId}, template {TemplateId} ({TemplateName}): enabled={IsEnabled}",
                clientId, templateId, template.Name, request.IsEnabled);
        }
        else
        {
            // Create new configuration
            config = await _attestationRepository.CreateAsync(new ClientAttestationTemplate
            {
                ClientId = clientId,
                TemplateId = templateId,
                IsEnabled = request.IsEnabled,
                Priority = request.Priority,
                Notes = request.Notes,
                UpdatedBy = updatedBy
            });

            _logger.LogInformation(
                "Created attestation template config for client {ClientId}, template {TemplateId} ({TemplateName})",
                clientId, templateId, template.Name);
        }

        // Reload with navigation properties
        config = await _attestationRepository.GetByIdAsync(config.Id)
            ?? throw new InvalidOperationException("Failed to reload configuration");

        return MapToDto(config, new AttestationTemplateDto(
            template.Id, template.Name, template.Description, template.TemplateType, template.IsActive));
    }

    public async Task DisableTemplateAsync(string clientId, int templateId, CancellationToken ct = default)
    {
        var config = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId);

        if (config != null)
        {
            await _attestationRepository.DeleteAsync(config.Id);
            _logger.LogInformation(
                "Disabled attestation template for client {ClientId}, template {TemplateId}",
                clientId, templateId);
        }
    }

    public async Task<ClientAttestationTemplateDto> ToggleTemplateAsync(
        string clientId,
        int templateId,
        string? updatedBy = null,
        CancellationToken ct = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new InvalidOperationException($"Email template {templateId} not found");

        var config = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId);

        if (config == null)
        {
            // Create and enable
            return await EnableTemplateAsync(clientId, templateId,
                new EnableTemplateRequest(IsEnabled: true), updatedBy, ct);
        }

        // Toggle existing
        config = await _attestationRepository.ToggleAsync(config.Id);

        _logger.LogInformation(
            "Toggled attestation template for client {ClientId}, template {TemplateId}: enabled={IsEnabled}",
            clientId, templateId, config.IsEnabled);

        return MapToDto(config, new AttestationTemplateDto(
            template.Id, template.Name, template.Description, template.TemplateType, template.IsActive));
    }

    public async Task<AssignedPolicyDto> AddPolicyAsync(
        string clientId,
        int templateId,
        Guid policyId,
        string? createdBy = null,
        CancellationToken ct = default)
    {
        // Ensure template config exists
        var config = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId)
            ?? throw new InvalidOperationException(
                $"No attestation template configuration exists for client {clientId} and template {templateId}. Enable the template first.");

        // Verify policy exists
        var policy = await _policyRepository.GetByIdAsync(policyId)
            ?? throw new InvalidOperationException($"Routing policy {policyId} not found");

        var assignment = await _attestationRepository.AddPolicyAsync(config.Id, policyId, createdBy);

        _logger.LogInformation(
            "Added policy {PolicyId} to attestation template for client {ClientId}, template {TemplateId}",
            policyId, clientId, templateId);

        return MapPolicyToDto(assignment);
    }

    public async Task RemovePolicyAsync(string clientId, int templateId, Guid policyId, CancellationToken ct = default)
    {
        var config = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId)
            ?? throw new InvalidOperationException(
                $"No attestation template configuration exists for client {clientId} and template {templateId}");

        await _attestationRepository.RemovePolicyAsync(config.Id, policyId);

        _logger.LogInformation(
            "Removed policy {PolicyId} from attestation template for client {ClientId}, template {TemplateId}",
            policyId, clientId, templateId);
    }

    public async Task<AssignedGroupDto> AddGroupAsync(
        string clientId,
        int templateId,
        Guid groupId,
        string role,
        string? createdBy = null,
        CancellationToken ct = default)
    {
        // Ensure template config exists
        var config = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId)
            ?? throw new InvalidOperationException(
                $"No attestation template configuration exists for client {clientId} and template {templateId}. Enable the template first.");

        // Verify group exists
        var group = await _groupRepository.GetByIdAsync(groupId)
            ?? throw new InvalidOperationException($"Recipient group {groupId} not found");

        var assignment = await _attestationRepository.AddGroupAsync(config.Id, groupId, role, createdBy);

        _logger.LogInformation(
            "Added group {GroupId} ({Role}) to attestation template for client {ClientId}, template {TemplateId}",
            groupId, role, clientId, templateId);

        return MapGroupToDto(assignment);
    }

    public async Task RemoveGroupAsync(string clientId, int templateId, Guid groupId, CancellationToken ct = default)
    {
        var config = await _attestationRepository.GetByClientAndTemplateAsync(clientId, templateId)
            ?? throw new InvalidOperationException(
                $"No attestation template configuration exists for client {clientId} and template {templateId}");

        await _attestationRepository.RemoveGroupAsync(config.Id, groupId);

        _logger.LogInformation(
            "Removed group {GroupId} from attestation template for client {ClientId}, template {TemplateId}",
            groupId, clientId, templateId);
    }

    private static ClientAttestationTemplateDto MapToDto(
        ClientAttestationTemplate config,
        AttestationTemplateDto template)
    {
        return new ClientAttestationTemplateDto(
            Id: config.Id,
            TemplateId: config.TemplateId,
            TemplateName: template.Name,
            TemplateDescription: template.Description,
            IsEnabled: config.IsEnabled,
            Priority: config.Priority,
            Notes: config.Notes,
            AssignedPolicies: config.Policies.Select(MapPolicyToDto).ToList(),
            AssignedGroups: config.Groups.Select(MapGroupToDto).ToList());
    }

    private static AssignedPolicyDto MapPolicyToDto(ClientAttestationTemplatePolicy assignment)
    {
        var policy = assignment.RoutingPolicy;
        return new AssignedPolicyDto(
            Id: assignment.RoutingPolicyId,
            Name: $"{policy?.Service}/{policy?.Topic}",
            Topic: policy?.Topic.ToString() ?? "Unknown",
            Service: policy?.Service.ToString() ?? "Unknown",
            GroupName: policy?.RecipientGroup?.Name);
    }

    private static AssignedGroupDto MapGroupToDto(ClientAttestationTemplateGroup assignment)
    {
        var group = assignment.RecipientGroup;
        return new AssignedGroupDto(
            Id: assignment.RecipientGroupId,
            Name: group?.Name ?? "Unknown",
            Role: assignment.Role.ToString(),
            MemberCount: group?.Memberships.Count ?? 0);
    }
}
