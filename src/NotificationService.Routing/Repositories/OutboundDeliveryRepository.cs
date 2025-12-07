using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for OutboundDelivery data access
/// </summary>
public class OutboundDeliveryRepository : IOutboundDeliveryRepository
{
    private readonly RoutingDbContext _context;

    public OutboundDeliveryRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<OutboundDelivery> CreateAsync(OutboundDelivery delivery)
    {
        delivery.CreatedAt = DateTime.UtcNow;
        _context.OutboundDeliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return delivery;
    }

    public async Task<List<OutboundDelivery>> CreateManyAsync(List<OutboundDelivery> deliveries)
    {
        var now = DateTime.UtcNow;
        foreach (var delivery in deliveries)
        {
            delivery.CreatedAt = now;
        }

        _context.OutboundDeliveries.AddRange(deliveries);
        await _context.SaveChangesAsync();
        return deliveries;
    }

    public async Task<OutboundDelivery?> GetByIdAsync(Guid id)
    {
        return await _context.OutboundDeliveries
            .Include(d => d.OutboundEvent)
            .Include(d => d.Contact)
            .Include(d => d.RoutingPolicy)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<OutboundDelivery>> GetByEventAsync(Guid eventId)
    {
        return await _context.OutboundDeliveries
            .Where(d => d.OutboundEventId == eventId)
            .Include(d => d.Contact)
            .OrderBy(d => d.Role)
            .ThenBy(d => d.Contact!.Name)
            .ToListAsync();
    }

    public async Task<List<OutboundDelivery>> GetPendingAsync(int limit = 100)
    {
        return await _context.OutboundDeliveries
            .Where(d => d.Status == DeliveryStatus.Pending)
            .Include(d => d.OutboundEvent)
            .Include(d => d.Contact)
            .OrderBy(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<OutboundDelivery>> GetFailedForRetryAsync(int limit = 100)
    {
        var now = DateTime.UtcNow;
        return await _context.OutboundDeliveries
            .Where(d => d.Status == DeliveryStatus.Failed
                     && d.NextRetryAt != null
                     && d.NextRetryAt <= now
                     && d.AttemptCount < 3)
            .Include(d => d.OutboundEvent)
            .Include(d => d.Contact)
            .OrderBy(d => d.NextRetryAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<OutboundDelivery>> GetByContactAsync(Guid contactId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.OutboundDeliveries
            .Where(d => d.ContactId == contactId);

        if (fromDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(d => d.CreatedAt <= toDate.Value);
        }

        return await query
            .Include(d => d.OutboundEvent)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<OutboundDelivery> UpdateAsync(OutboundDelivery delivery)
    {
        _context.OutboundDeliveries.Update(delivery);
        await _context.SaveChangesAsync();
        return delivery;
    }

    public async Task UpdateStatusAsync(Guid id, DeliveryStatus status, string? errorMessage = null)
    {
        var delivery = await _context.OutboundDeliveries.FindAsync(id)
            ?? throw new InvalidOperationException($"Delivery {id} not found");

        delivery.Status = status;

        switch (status)
        {
            case DeliveryStatus.Processing:
                delivery.AttemptCount++;
                break;
            case DeliveryStatus.Delivered:
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.SentAt ??= DateTime.UtcNow;
                break;
            case DeliveryStatus.Failed:
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = errorMessage;
                // Schedule retry with exponential backoff
                if (delivery.AttemptCount < 3)
                {
                    delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, delivery.AttemptCount));
                }
                break;
        }

        await _context.SaveChangesAsync();
    }
}
