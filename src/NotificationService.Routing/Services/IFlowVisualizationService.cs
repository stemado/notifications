using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.DTOs;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service interface for notification flow visualization
/// </summary>
public interface IFlowVisualizationService
{
    /// <summary>
    /// Get complete flow data for a specific policy
    /// </summary>
    Task<FlowData?> GetFlowForPolicyAsync(Guid policyId);

    /// <summary>
    /// Simulate routing for given service/topic/client combination
    /// </summary>
    Task<FlowData?> SimulateFlowAsync(SourceService service, NotificationTopic topic, string? clientId);
}
