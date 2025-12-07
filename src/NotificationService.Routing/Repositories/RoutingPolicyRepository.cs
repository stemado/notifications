using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for RoutingPolicy data access
/// </summary>
public class RoutingPolicyRepository : IRoutingPolicyRepository
{
    private readonly RoutingDbContext _context;

    public RoutingPolicyRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<RoutingPolicy> CreateAsync(RoutingPolicy policy)
    {
        policy.CreatedAt = DateTime.UtcNow;
        policy.UpdatedAt = DateTime.UtcNow;
        _context.RoutingPolicies.Add(policy);
        await _context.SaveChangesAsync();
        return policy;
    }

    public async Task<RoutingPolicy?> GetByIdAsync(Guid id)
    {
        return await _context.RoutingPolicies
            .Include(p => p.RecipientGroup)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<RoutingPolicy>> GetByClientAsync(string? clientId)
    {
        return await _context.RoutingPolicies
            .Where(p => p.ClientId == clientId)
            .Include(p => p.RecipientGroup)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Service)
            .ThenBy(p => p.Topic)
            .ToListAsync();
    }

    public async Task<List<RoutingPolicy>> GetByServiceAndTopicAsync(SourceService service, NotificationTopic topic)
    {
        return await _context.RoutingPolicies
            .Where(p => p.Service == service && p.Topic == topic && p.IsEnabled)
            .Include(p => p.RecipientGroup)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<List<RoutingPolicy>> GetMatchingPoliciesAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId,
        NotificationSeverity severity)
    {
        // First try to find client-specific policies
        var clientPolicies = await _context.RoutingPolicies
            .Where(p => p.Service == service
                     && p.Topic == topic
                     && p.ClientId == clientId
                     && p.IsEnabled
                     && (p.MinSeverity == null || severity >= p.MinSeverity))
            .Include(p => p.RecipientGroup)
                .ThenInclude(g => g!.Memberships)
                    .ThenInclude(m => m.Contact)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();

        if (clientPolicies.Count > 0)
        {
            return clientPolicies;
        }

        // Fall back to default policies (ClientId = null)
        return await _context.RoutingPolicies
            .Where(p => p.Service == service
                     && p.Topic == topic
                     && p.ClientId == null
                     && p.IsEnabled
                     && (p.MinSeverity == null || severity >= p.MinSeverity))
            .Include(p => p.RecipientGroup)
                .ThenInclude(g => g!.Memberships)
                    .ThenInclude(m => m.Contact)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<List<RoutingPolicy>> GetAllAsync(bool includeDisabled = false)
    {
        var query = _context.RoutingPolicies.AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(p => p.IsEnabled);
        }

        return await query
            .Include(p => p.RecipientGroup)
            .OrderBy(p => p.ClientId)
            .ThenByDescending(p => p.Priority)
            .ThenBy(p => p.Service)
            .ThenBy(p => p.Topic)
            .ToListAsync();
    }

    public async Task<RoutingPolicy> UpdateAsync(RoutingPolicy policy)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        _context.RoutingPolicies.Update(policy);
        await _context.SaveChangesAsync();
        return policy;
    }

    public async Task DeleteAsync(Guid id)
    {
        var policy = await _context.RoutingPolicies.FindAsync(id)
            ?? throw new InvalidOperationException($"Policy {id} not found");

        _context.RoutingPolicies.Remove(policy);
        await _context.SaveChangesAsync();
    }

    public async Task<RoutingPolicy> ToggleAsync(Guid id)
    {
        var policy = await _context.RoutingPolicies.FindAsync(id)
            ?? throw new InvalidOperationException($"Policy {id} not found");

        policy.IsEnabled = !policy.IsEnabled;
        policy.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return policy;
    }
}
