using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for TopicTemplateMapping data access
/// </summary>
public class TopicTemplateMappingRepository : ITopicTemplateMappingRepository
{
    private readonly RoutingDbContext _context;

    public TopicTemplateMappingRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<TopicTemplateMapping?> GetMappingAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId)
    {
        // First try to find a client-specific mapping
        if (!string.IsNullOrEmpty(clientId))
        {
            var clientMapping = await _context.TopicTemplateMappings
                .Where(m => m.Service == service
                         && m.Topic == topic
                         && m.ClientId == clientId
                         && m.IsEnabled)
                .OrderByDescending(m => m.Priority)
                .FirstOrDefaultAsync();

            if (clientMapping != null)
            {
                return clientMapping;
            }
        }

        // Fall back to default mapping (ClientId = null)
        return await _context.TopicTemplateMappings
            .Where(m => m.Service == service
                     && m.Topic == topic
                     && m.ClientId == null
                     && m.IsEnabled)
            .OrderByDescending(m => m.Priority)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TopicTemplateMapping>> GetAllAsync(bool includeDisabled = false)
    {
        var query = _context.TopicTemplateMappings.AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(m => m.IsEnabled);
        }

        return await query
            .OrderBy(m => m.Service)
            .ThenBy(m => m.Topic)
            .ThenBy(m => m.ClientId)
            .ThenByDescending(m => m.Priority)
            .ToListAsync();
    }

    public async Task<TopicTemplateMapping?> GetByIdAsync(Guid id)
    {
        return await _context.TopicTemplateMappings.FindAsync(id);
    }

    public async Task<TopicTemplateMapping> CreateAsync(TopicTemplateMapping mapping)
    {
        mapping.CreatedAt = DateTime.UtcNow;
        mapping.UpdatedAt = DateTime.UtcNow;
        _context.TopicTemplateMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<TopicTemplateMapping> UpdateAsync(TopicTemplateMapping mapping)
    {
        mapping.UpdatedAt = DateTime.UtcNow;
        _context.TopicTemplateMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteAsync(Guid id)
    {
        var mapping = await _context.TopicTemplateMappings.FindAsync(id)
            ?? throw new InvalidOperationException($"Mapping {id} not found");

        _context.TopicTemplateMappings.Remove(mapping);
        await _context.SaveChangesAsync();
    }

    public async Task<TopicTemplateMapping> ToggleAsync(Guid id)
    {
        var mapping = await _context.TopicTemplateMappings.FindAsync(id)
            ?? throw new InvalidOperationException($"Mapping {id} not found");

        mapping.IsEnabled = !mapping.IsEnabled;
        mapping.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return mapping;
    }

    public async Task<List<TopicTemplateMapping>> GetByTemplateIdAsync(int templateId)
    {
        return await _context.TopicTemplateMappings
            .Where(m => m.TemplateId == templateId)
            .OrderBy(m => m.Service)
            .ThenBy(m => m.Topic)
            .ToListAsync();
    }
}
