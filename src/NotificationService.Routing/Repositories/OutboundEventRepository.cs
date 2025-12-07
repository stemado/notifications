using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for OutboundEvent data access
/// </summary>
public class OutboundEventRepository : IOutboundEventRepository
{
    private readonly RoutingDbContext _context;

    public OutboundEventRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<OutboundEvent> CreateAsync(OutboundEvent evt)
    {
        evt.CreatedAt = DateTime.UtcNow;
        _context.OutboundEvents.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task<OutboundEvent?> GetByIdAsync(Guid id)
    {
        return await _context.OutboundEvents
            .Include(e => e.Deliveries)
                .ThenInclude(d => d.Contact)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<OutboundEvent>> GetUnprocessedAsync(int limit = 100)
    {
        return await _context.OutboundEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<OutboundEvent>> GetBySagaAsync(Guid sagaId)
    {
        return await _context.OutboundEvents
            .Where(e => e.SagaId == sagaId)
            .Include(e => e.Deliveries)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<OutboundEvent>> GetByClientAsync(string clientId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.OutboundEvents
            .Where(e => e.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= toDate.Value);
        }

        return await query
            .Include(e => e.Deliveries)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<OutboundEvent> UpdateAsync(OutboundEvent evt)
    {
        _context.OutboundEvents.Update(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task MarkProcessedAsync(Guid id)
    {
        var evt = await _context.OutboundEvents.FindAsync(id)
            ?? throw new InvalidOperationException($"Event {id} not found");

        evt.ProcessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
