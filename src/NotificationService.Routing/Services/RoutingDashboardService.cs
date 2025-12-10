using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Data;
using NotificationService.Routing.DTOs;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service for aggregating routing dashboard data
/// </summary>
public class RoutingDashboardService : IRoutingDashboardService
{
    private readonly RoutingDbContext _dbContext;
    private readonly ILogger<RoutingDashboardService> _logger;

    public RoutingDashboardService(RoutingDbContext dbContext, ILogger<RoutingDashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RoutingDashboardData> GetDashboardDataAsync()
    {
        var statistics = await GetStatisticsAsync();
        var recentEvents = await GetRecentEventsAsync(10);
        var failedDeliveries = await GetFailedDeliveriesAsync(10);
        var topClients = await GetTopClientsAsync(10);

        return new RoutingDashboardData
        {
            Statistics = statistics,
            RecentEvents = recentEvents,
            FailedDeliveries = failedDeliveries,
            TopClients = topClients
        };
    }

    public async Task<RoutingStatistics> GetStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;

        // Get contact counts
        var totalContacts = await _dbContext.Contacts.CountAsync();
        var activeContacts = await _dbContext.Contacts.CountAsync(c => c.IsActive);

        // Get group counts
        var totalGroups = await _dbContext.RecipientGroups.CountAsync();
        var activeGroups = await _dbContext.RecipientGroups.CountAsync(g => g.IsActive);

        // Get policy counts
        var totalPolicies = await _dbContext.RoutingPolicies.CountAsync();
        var enabledPolicies = await _dbContext.RoutingPolicies.CountAsync(p => p.IsEnabled);

        // Get today's event counts
        var eventsToday = await _dbContext.OutboundEvents
            .CountAsync(e => e.CreatedAt >= today);

        // Get today's delivery counts
        var deliveriesToday = await _dbContext.OutboundDeliveries
            .CountAsync(d => d.CreatedAt >= today);

        var deliveredToday = await _dbContext.OutboundDeliveries
            .CountAsync(d => d.CreatedAt >= today && d.Status == DeliveryStatus.Delivered);

        // Get pending and failed counts
        var pendingDeliveries = await _dbContext.OutboundDeliveries
            .CountAsync(d => d.Status == DeliveryStatus.Pending);

        var failedDeliveries = await _dbContext.OutboundDeliveries
            .CountAsync(d => d.Status == DeliveryStatus.Failed);

        // Calculate success rate for today
        var successRateToday = deliveriesToday > 0
            ? (double)deliveredToday / deliveriesToday * 100
            : 100;

        return new RoutingStatistics
        {
            TotalContacts = totalContacts,
            ActiveContacts = activeContacts,
            TotalGroups = totalGroups,
            ActiveGroups = activeGroups,
            TotalPolicies = totalPolicies,
            EnabledPolicies = enabledPolicies,
            EventsToday = eventsToday,
            DeliveriesToday = deliveriesToday,
            SuccessRateToday = Math.Round(successRateToday, 1),
            PendingDeliveries = pendingDeliveries,
            FailedDeliveries = failedDeliveries
        };
    }

    public async Task<List<OutboundEventSummary>> GetRecentEventsAsync(int limit = 10)
    {
        var events = await _dbContext.OutboundEvents
            .Include(e => e.Deliveries)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return events.Select(e => new OutboundEventSummary
        {
            Id = e.Id,
            Service = e.Service.ToString(),
            Topic = e.Topic.ToString(),
            ClientId = e.ClientId,
            Severity = e.Severity.ToString(),
            Subject = e.Subject,
            SagaId = e.SagaId,
            CreatedAt = e.CreatedAt,
            ProcessedAt = e.ProcessedAt,
            DeliveryCount = e.Deliveries?.Count ?? 0,
            PendingCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Pending) ?? 0,
            DeliveredCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Delivered) ?? 0,
            FailedCount = e.Deliveries?.Count(d => d.Status == DeliveryStatus.Failed) ?? 0
        }).ToList();
    }

    public async Task<List<OutboundDeliverySummary>> GetFailedDeliveriesAsync(int limit = 10)
    {
        var deliveries = await _dbContext.OutboundDeliveries
            .Include(d => d.Contact)
            .Where(d => d.Status == DeliveryStatus.Failed)
            .OrderByDescending(d => d.FailedAt ?? d.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return deliveries.Select(d => new OutboundDeliverySummary
        {
            Id = d.Id,
            EventId = d.OutboundEventId,
            ContactName = d.Contact?.Name ?? "Unknown",
            ContactEmail = d.Contact?.Email ?? "Unknown",
            Channel = d.Channel.ToString(),
            Status = d.Status.ToString(),
            CreatedAt = d.CreatedAt,
            ErrorMessage = d.ErrorMessage
        }).ToList();
    }

    public async Task<List<ClientRoutingStatistics>> GetTopClientsAsync(int limit = 10)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Get distinct clients from policies
        var clientIds = await _dbContext.RoutingPolicies
            .Where(p => p.ClientId != null)
            .Select(p => p.ClientId!)
            .Distinct()
            .ToListAsync();

        var clientStats = new List<ClientRoutingStatistics>();

        foreach (var clientId in clientIds.Take(limit))
        {
            // Get group count for client
            var totalGroups = await _dbContext.RecipientGroups
                .CountAsync(g => g.ClientId == clientId);

            // Get policy counts for client
            var totalPolicies = await _dbContext.RoutingPolicies
                .CountAsync(p => p.ClientId == clientId);
            var activePolicies = await _dbContext.RoutingPolicies
                .CountAsync(p => p.ClientId == clientId && p.IsEnabled);

            // Get events in last 7 days
            var eventsLast7Days = await _dbContext.OutboundEvents
                .CountAsync(e => e.ClientId == clientId && e.CreatedAt >= sevenDaysAgo);

            // Get deliveries in last 7 days
            var deliveriesLast7Days = await _dbContext.OutboundDeliveries
                .Include(d => d.OutboundEvent)
                .CountAsync(d => d.OutboundEvent != null && d.OutboundEvent.ClientId == clientId && d.CreatedAt >= sevenDaysAgo);

            var deliveredLast7Days = await _dbContext.OutboundDeliveries
                .Include(d => d.OutboundEvent)
                .CountAsync(d => d.OutboundEvent != null && d.OutboundEvent.ClientId == clientId
                    && d.CreatedAt >= sevenDaysAgo
                    && d.Status == DeliveryStatus.Delivered);

            // Get unique contacts for client
            var uniqueContacts = await _dbContext.GroupMemberships
                .Include(m => m.Group)
                .Where(m => m.Group != null && m.Group.ClientId == clientId)
                .Select(m => m.ContactId)
                .Distinct()
                .CountAsync();

            var successRate = deliveriesLast7Days > 0
                ? (double)deliveredLast7Days / deliveriesLast7Days * 100
                : 100;

            clientStats.Add(new ClientRoutingStatistics
            {
                ClientId = clientId,
                TotalGroups = totalGroups,
                TotalPolicies = totalPolicies,
                ActivePolicies = activePolicies,
                UniqueContacts = uniqueContacts,
                EventsLast7Days = eventsLast7Days,
                DeliveriesLast7Days = deliveriesLast7Days,
                SuccessRate = Math.Round(successRate, 1)
            });
        }

        // Sort by activity (events in last 7 days)
        return clientStats.OrderByDescending(c => c.EventsLast7Days).ToList();
    }
}
