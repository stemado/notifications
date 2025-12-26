using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository interface for TopicTemplateMapping data access
/// </summary>
public interface ITopicTemplateMappingRepository
{
    /// <summary>
    /// Get the best matching template mapping for the given service/topic/client combination.
    /// Client-specific mappings take priority over default mappings (clientId = null).
    /// </summary>
    Task<TopicTemplateMapping?> GetMappingAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId);

    /// <summary>
    /// Get all mappings, optionally including disabled ones
    /// </summary>
    Task<List<TopicTemplateMapping>> GetAllAsync(bool includeDisabled = false);

    /// <summary>
    /// Get a mapping by its ID
    /// </summary>
    Task<TopicTemplateMapping?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new mapping
    /// </summary>
    Task<TopicTemplateMapping> CreateAsync(TopicTemplateMapping mapping);

    /// <summary>
    /// Update an existing mapping
    /// </summary>
    Task<TopicTemplateMapping> UpdateAsync(TopicTemplateMapping mapping);

    /// <summary>
    /// Delete a mapping by ID
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggle the enabled status of a mapping
    /// </summary>
    Task<TopicTemplateMapping> ToggleAsync(Guid id);

    /// <summary>
    /// Get all mappings for a specific template ID
    /// </summary>
    Task<List<TopicTemplateMapping>> GetByTemplateIdAsync(int templateId);
}
