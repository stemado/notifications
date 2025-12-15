using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for test email delivery records
/// </summary>
public class TestEmailDeliveryRepository : ITestEmailDeliveryRepository
{
    private readonly RoutingDbContext _context;

    public TestEmailDeliveryRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<TestEmailDelivery> CreateAsync(TestEmailDelivery delivery)
    {
        delivery.Id = Guid.NewGuid();
        delivery.SentAt = DateTime.UtcNow;

        _context.TestEmailDeliveries.Add(delivery);
        await _context.SaveChangesAsync();

        return delivery;
    }

    public async Task<TestEmailDelivery?> GetByIdAsync(Guid id)
    {
        return await _context.TestEmailDeliveries
            .Include(d => d.RecipientGroup)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<TestEmailDelivery>> GetByGroupIdAsync(Guid groupId, int limit = 50)
    {
        return await _context.TestEmailDeliveries
            .Include(d => d.RecipientGroup)
            .Where(d => d.RecipientGroupId == groupId)
            .OrderByDescending(d => d.SentAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<TestEmailDelivery>> GetByInitiatorAsync(string initiatedBy, int limit = 50)
    {
        return await _context.TestEmailDeliveries
            .Include(d => d.RecipientGroup)
            .Where(d => d.InitiatedBy == initiatedBy)
            .OrderByDescending(d => d.SentAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<TestEmailDelivery>> GetRecentAsync(int limit = 50)
    {
        return await _context.TestEmailDeliveries
            .Include(d => d.RecipientGroup)
            .OrderByDescending(d => d.SentAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(List<TestEmailDelivery> Items, int TotalCount)> GetPagedAsync(
        Guid? groupId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? initiatedBy = null,
        bool? successOnly = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.TestEmailDeliveries
            .Include(d => d.RecipientGroup)
            .AsQueryable();

        if (groupId.HasValue)
        {
            query = query.Where(d => d.RecipientGroupId == groupId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(d => d.SentAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(d => d.SentAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(initiatedBy))
        {
            query = query.Where(d => d.InitiatedBy == initiatedBy);
        }

        if (successOnly.HasValue)
        {
            query = query.Where(d => d.Success == successOnly.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
