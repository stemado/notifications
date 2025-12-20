using NotificationService.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository interface for OutboundDelivery data access
/// </summary>
public interface IOutboundDeliveryRepository
{
    Task<OutboundDelivery> CreateAsync(OutboundDelivery delivery);
    Task<List<OutboundDelivery>> CreateManyAsync(List<OutboundDelivery> deliveries);
    Task<OutboundDelivery?> GetByIdAsync(Guid id);
    Task<List<OutboundDelivery>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<List<OutboundDelivery>> GetByEventAsync(Guid eventId);
    Task<List<OutboundDelivery>> GetPendingAsync(int limit = 100);
    Task<List<OutboundDelivery>> GetFailedForRetryAsync(int limit = 100);
    Task<List<OutboundDelivery>> GetByContactAsync(Guid contactId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<OutboundDelivery> UpdateAsync(OutboundDelivery delivery);
    Task UpdateStatusAsync(Guid id, DeliveryStatus status, string? errorMessage = null, string? externalMessageId = null);
}
