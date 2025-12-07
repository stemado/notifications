using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository interface for OutboundEvent data access
/// </summary>
public interface IOutboundEventRepository
{
    Task<OutboundEvent> CreateAsync(OutboundEvent evt);
    Task<OutboundEvent?> GetByIdAsync(Guid id);
    Task<List<OutboundEvent>> GetUnprocessedAsync(int limit = 100);
    Task<List<OutboundEvent>> GetBySagaAsync(Guid sagaId);
    Task<List<OutboundEvent>> GetByClientAsync(string clientId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<OutboundEvent> UpdateAsync(OutboundEvent evt);
    Task MarkProcessedAsync(Guid id);
}
