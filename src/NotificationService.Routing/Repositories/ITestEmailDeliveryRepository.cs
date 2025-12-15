using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository interface for test email delivery records
/// </summary>
public interface ITestEmailDeliveryRepository
{
    Task<TestEmailDelivery> CreateAsync(TestEmailDelivery delivery);
    Task<TestEmailDelivery?> GetByIdAsync(Guid id);
    Task<List<TestEmailDelivery>> GetByGroupIdAsync(Guid groupId, int limit = 50);
    Task<List<TestEmailDelivery>> GetByInitiatorAsync(string initiatedBy, int limit = 50);
    Task<List<TestEmailDelivery>> GetRecentAsync(int limit = 50);
    Task<(List<TestEmailDelivery> Items, int TotalCount)> GetPagedAsync(
        Guid? groupId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? initiatedBy = null,
        bool? successOnly = null,
        int page = 1,
        int pageSize = 20);
}
