using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service implementation for topic registry management
/// </summary>
public class TopicService : ITopicService
{
    private readonly RoutingDbContext _dbContext;

    public TopicService(RoutingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Topic> CreateAsync(Topic topic)
    {
        topic.CreatedAt = DateTime.UtcNow;
        topic.UpdatedAt = DateTime.UtcNow;

        _dbContext.Topics.Add(topic);
        await _dbContext.SaveChangesAsync();

        return topic;
    }

    public async Task<Topic?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Topics.FindAsync(id);
    }

    public async Task<Topic?> GetByServiceAndTopicAsync(SourceService service, NotificationTopic topicName)
    {
        return await _dbContext.Topics
            .FirstOrDefaultAsync(t => t.Service == service && t.TopicName == topicName);
    }

    public async Task<List<Topic>> GetByServiceAsync(SourceService service)
    {
        return await _dbContext.Topics
            .Where(t => t.Service == service && t.IsActive)
            .OrderBy(t => t.DisplayName)
            .ToListAsync();
    }

    public async Task<List<Topic>> GetAllAsync(bool includeInactive = false)
    {
        var query = _dbContext.Topics.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query
            .OrderBy(t => t.Service)
            .ThenBy(t => t.DisplayName)
            .ToListAsync();
    }

    public async Task<Topic> UpdateAsync(Topic topic)
    {
        topic.UpdatedAt = DateTime.UtcNow;

        _dbContext.Topics.Update(topic);
        await _dbContext.SaveChangesAsync();

        return topic;
    }

    public async Task DeleteAsync(Guid id)
    {
        var topic = await _dbContext.Topics.FindAsync(id);
        if (topic != null)
        {
            _dbContext.Topics.Remove(topic);
            await _dbContext.SaveChangesAsync();
        }
    }
}
