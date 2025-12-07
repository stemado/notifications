namespace NotificationService.Routing.DTOs;

/// <summary>
/// Complete routing configuration for a client
/// </summary>
public record ClientRoutingConfiguration
{
    public required string ClientId { get; init; }

    /// <summary>
    /// Recipient groups specific to this client
    /// </summary>
    public List<RecipientGroupSummary> Groups { get; init; } = new();

    /// <summary>
    /// Routing policies for this client (including default fallback policies)
    /// </summary>
    public List<RoutingPolicySummary> Policies { get; init; } = new();

    /// <summary>
    /// Unique contacts receiving notifications for this client
    /// </summary>
    public List<ContactSummary> Contacts { get; init; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public ClientRoutingStats Stats { get; init; } = new();
}

/// <summary>
/// Summary statistics for client routing configuration
/// </summary>
public record ClientRoutingStats
{
    public int TotalGroups { get; init; }
    public int TotalPolicies { get; init; }
    public int ActivePolicies { get; init; }
    public int UniqueContacts { get; init; }
    public int PolicyCoverageByTopic { get; init; }
}
