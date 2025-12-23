namespace NotificationService.Client.Routing.Models;

/// <summary>
/// Summary of a routing policy
/// </summary>
public record RoutingPolicySummary
{
    public Guid Id { get; init; }
    public string Service { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string? ClientId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public int ContactCount { get; init; }
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Client routing configuration summary
/// </summary>
public record ClientRoutingConfiguration
{
    public string ClientId { get; init; } = string.Empty;
    public List<RoutingPolicySummary> Policies { get; init; } = new();
    public List<ContactSummary> Contacts { get; init; } = new();
    public RoutingStats Stats { get; init; } = new();
}

/// <summary>
/// Contact summary for routing
/// </summary>
public record ContactSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

/// <summary>
/// Routing statistics
/// </summary>
public record RoutingStats
{
    public int TotalPolicies { get; init; }
    public int ActivePolicies { get; init; }
    public int TotalContacts { get; init; }
    public int ActiveContacts { get; init; }
}
