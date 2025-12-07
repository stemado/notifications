using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service interface for routing policy management
/// </summary>
public interface IRoutingPolicyService
{
    Task<RoutingPolicy> CreateAsync(RoutingPolicy policy);
    Task<RoutingPolicy?> GetByIdAsync(Guid id);
    Task<List<RoutingPolicy>> GetByClientAsync(string? clientId);
    Task<List<RoutingPolicy>> GetByServiceAndTopicAsync(SourceService service, NotificationTopic topic);
    Task<List<RoutingPolicy>> GetAllAsync(bool includeDisabled = false);
    Task<RoutingPolicy> UpdateAsync(RoutingPolicy policy);
    Task DeleteAsync(Guid id);
    Task<RoutingPolicy> ToggleAsync(Guid id);
}
