using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service interface for topic registry management
/// </summary>
public interface ITopicService
{
    Task<Topic> CreateAsync(Topic topic);
    Task<Topic?> GetByIdAsync(Guid id);
    Task<Topic?> GetByServiceAndTopicAsync(SourceService service, NotificationTopic topicName);
    Task<List<Topic>> GetByServiceAsync(SourceService service);
    Task<List<Topic>> GetAllAsync(bool includeInactive = false);
    Task<Topic> UpdateAsync(Topic topic);
    Task DeleteAsync(Guid id);
}
