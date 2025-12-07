using Microsoft.Extensions.Logging;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service implementation for routing policy management
/// </summary>
public class RoutingPolicyService : IRoutingPolicyService
{
    private readonly IRoutingPolicyRepository _repository;
    private readonly IRecipientGroupRepository _groupRepository;
    private readonly ILogger<RoutingPolicyService> _logger;

    public RoutingPolicyService(
        IRoutingPolicyRepository repository,
        IRecipientGroupRepository groupRepository,
        ILogger<RoutingPolicyService> logger)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task<RoutingPolicy> CreateAsync(RoutingPolicy policy)
    {
        // Verify recipient group exists
        var group = await _groupRepository.GetByIdAsync(policy.RecipientGroupId)
            ?? throw new InvalidOperationException($"Recipient group {policy.RecipientGroupId} not found");

        var created = await _repository.CreateAsync(policy);
        _logger.LogInformation(
            "Created routing policy {PolicyId}: {Service}/{Topic} for client {ClientId} -> {GroupName} ({Role})",
            created.Id, created.Service, created.Topic, created.ClientId ?? "(default)",
            group.Name, created.Role);
        return created;
    }

    public async Task<RoutingPolicy?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<RoutingPolicy>> GetByClientAsync(string? clientId)
    {
        return await _repository.GetByClientAsync(clientId);
    }

    public async Task<List<RoutingPolicy>> GetByServiceAndTopicAsync(SourceService service, NotificationTopic topic)
    {
        return await _repository.GetByServiceAndTopicAsync(service, topic);
    }

    public async Task<List<RoutingPolicy>> GetAllAsync(bool includeDisabled = false)
    {
        return await _repository.GetAllAsync(includeDisabled);
    }

    public async Task<RoutingPolicy> UpdateAsync(RoutingPolicy policy)
    {
        var updated = await _repository.UpdateAsync(policy);
        _logger.LogInformation("Updated routing policy {PolicyId}", policy.Id);
        return updated;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Deleted routing policy {PolicyId}", id);
    }

    public async Task<RoutingPolicy> ToggleAsync(Guid id)
    {
        var toggled = await _repository.ToggleAsync(id);
        _logger.LogInformation("Toggled routing policy {PolicyId} to {IsEnabled}", id, toggled.IsEnabled);
        return toggled;
    }
}
