namespace NotificationService.Routing.DTOs;

/// <summary>
/// Dashboard data for the routing overview page
/// </summary>
public record RoutingDashboardData
{
    public required RoutingStatistics Statistics { get; init; }
    public List<OutboundEventSummary> RecentEvents { get; init; } = new();
    public List<OutboundDeliverySummary> FailedDeliveries { get; init; } = new();
    public List<ClientRoutingStatistics> TopClients { get; init; } = new();
}

/// <summary>
/// Aggregate statistics for the routing dashboard
/// </summary>
public record RoutingStatistics
{
    public int TotalContacts { get; init; }
    public int ActiveContacts { get; init; }
    public int TotalGroups { get; init; }
    public int ActiveGroups { get; init; }
    public int TotalPolicies { get; init; }
    public int EnabledPolicies { get; init; }
    public int EventsToday { get; init; }
    public int DeliveriesToday { get; init; }
    public double SuccessRateToday { get; init; }
    public int PendingDeliveries { get; init; }
    public int FailedDeliveries { get; init; }
}

/// <summary>
/// Summary of an outbound delivery for dashboard lists
/// </summary>
public record OutboundDeliverySummary
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public required string ContactName { get; init; }
    public required string ContactEmail { get; init; }
    public required string Channel { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Client-specific routing statistics for dashboard
/// </summary>
public record ClientRoutingStatistics
{
    public required string ClientId { get; init; }
    public int TotalGroups { get; init; }
    public int TotalPolicies { get; init; }
    public int ActivePolicies { get; init; }
    public int UniqueContacts { get; init; }
    public int EventsLast7Days { get; init; }
    public int DeliveriesLast7Days { get; init; }
    public double SuccessRate { get; init; }
}
