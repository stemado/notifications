using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// Request to create a new routing policy
/// </summary>
public record CreateRoutingPolicyRequest
{
    public required SourceService Service { get; init; }
    public required NotificationTopic Topic { get; init; }
    public string? ClientId { get; init; }
    public NotificationSeverity? MinSeverity { get; init; }
    public required NotificationChannel Channel { get; init; }
    public required Guid RecipientGroupId { get; init; }
    public required DeliveryRole Role { get; init; }
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Request to update an existing routing policy
/// </summary>
public record UpdateRoutingPolicyRequest
{
    public NotificationSeverity? MinSeverity { get; init; }
    public required NotificationChannel Channel { get; init; }
    public required Guid RecipientGroupId { get; init; }
    public required DeliveryRole Role { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Summary view of a routing policy
/// </summary>
public record RoutingPolicySummary
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string Topic { get; init; }
    public string? ClientId { get; init; }
    public string? MinSeverity { get; init; }
    public required string Channel { get; init; }
    public Guid RecipientGroupId { get; init; }
    public required string RecipientGroupName { get; init; }
    public required string Role { get; init; }
    public int Priority { get; init; }
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Detailed view of a routing policy
/// </summary>
public record RoutingPolicyDetails
{
    public Guid Id { get; init; }
    public required string Service { get; init; }
    public required string Topic { get; init; }
    public string? ClientId { get; init; }
    public string? MinSeverity { get; init; }
    public required string Channel { get; init; }
    public Guid RecipientGroupId { get; init; }
    public required string RecipientGroupName { get; init; }
    public int RecipientCount { get; init; }
    public required string Role { get; init; }
    public int Priority { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
