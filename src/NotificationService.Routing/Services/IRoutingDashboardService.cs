using NotificationService.Routing.DTOs;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service interface for routing dashboard data aggregation
/// </summary>
public interface IRoutingDashboardService
{
    /// <summary>
    /// Get aggregated dashboard data including statistics, recent events, and failures
    /// </summary>
    Task<RoutingDashboardData> GetDashboardDataAsync();

    /// <summary>
    /// Get routing statistics only
    /// </summary>
    Task<RoutingStatistics> GetStatisticsAsync();

    /// <summary>
    /// Get recent outbound events
    /// </summary>
    Task<List<OutboundEventSummary>> GetRecentEventsAsync(int limit = 10);

    /// <summary>
    /// Get failed deliveries
    /// </summary>
    Task<List<OutboundDeliverySummary>> GetFailedDeliveriesAsync(int limit = 10);

    /// <summary>
    /// Get top clients by activity
    /// </summary>
    Task<List<ClientRoutingStatistics>> GetTopClientsAsync(int limit = 10);
}
