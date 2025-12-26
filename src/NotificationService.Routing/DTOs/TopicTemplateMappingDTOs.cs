using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// DTO for displaying a topic-to-template mapping
/// </summary>
public record TopicTemplateMappingDto
{
    public Guid Id { get; init; }
    public SourceService Service { get; init; }
    public NotificationTopic Topic { get; init; }
    public string? ClientId { get; init; }
    public int TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public bool IsEnabled { get; init; }
    public int Priority { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// Request to create a new topic-to-template mapping
/// </summary>
public record CreateTopicTemplateMappingRequest
{
    /// <summary>
    /// The source service this mapping applies to
    /// </summary>
    public required SourceService Service { get; init; }

    /// <summary>
    /// The notification topic this mapping applies to
    /// </summary>
    public required NotificationTopic Topic { get; init; }

    /// <summary>
    /// Optional client ID for client-specific mappings.
    /// If null, this is the default mapping for all clients.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// The email template ID to use for this combination
    /// </summary>
    public required int TemplateId { get; init; }

    /// <summary>
    /// Priority for resolution when multiple mappings match (higher wins)
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Whether this mapping is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Request to update an existing topic-to-template mapping
/// </summary>
public record UpdateTopicTemplateMappingRequest
{
    /// <summary>
    /// The email template ID to use
    /// </summary>
    public required int TemplateId { get; init; }

    /// <summary>
    /// Priority for resolution (higher wins)
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// Whether this mapping is enabled
    /// </summary>
    public bool? IsEnabled { get; init; }
}

/// <summary>
/// Response after toggling mapping status
/// </summary>
public record ToggleMappingResponse
{
    public Guid Id { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Response for checking template resolution for a given service/topic
/// </summary>
public record TemplateResolutionPreviewResponse
{
    public SourceService Service { get; init; }
    public NotificationTopic Topic { get; init; }
    public string? ClientId { get; init; }
    public bool HasMapping { get; init; }
    public TopicTemplateMappingDto? Mapping { get; init; }
    public string? TemplateName { get; init; }
    public string? TemplateSubject { get; init; }
}
