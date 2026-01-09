using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Repositories;

namespace NotificationService.Routing.Services;

/// <summary>
/// Service for building notification flow visualization data.
/// Aggregates policy, template, topic, and recipient data for flow diagrams.
/// </summary>
public class FlowVisualizationService : IFlowVisualizationService
{
    private readonly RoutingDbContext _dbContext;
    private readonly ITopicTemplateMappingRepository _mappingRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IRecipientGroupService _groupService;
    private readonly ILogger<FlowVisualizationService> _logger;

    public FlowVisualizationService(
        RoutingDbContext dbContext,
        ITopicTemplateMappingRepository mappingRepository,
        IEmailTemplateRepository templateRepository,
        IRecipientGroupService groupService,
        ILogger<FlowVisualizationService> logger)
    {
        _dbContext = dbContext;
        _mappingRepository = mappingRepository;
        _templateRepository = templateRepository;
        _groupService = groupService;
        _logger = logger;
    }

    public async Task<FlowData?> GetFlowForPolicyAsync(Guid policyId)
    {
        // Get the policy
        var policy = await _dbContext.RoutingPolicies
            .Include(p => p.RecipientGroup)
            .FirstOrDefaultAsync(p => p.Id == policyId);

        if (policy == null)
        {
            _logger.LogWarning("Policy {PolicyId} not found for flow visualization", policyId);
            return null;
        }

        return await BuildFlowDataAsync(
            policy.Service,
            policy.Topic,
            policy.ClientId,
            policy.Channel.ToString(),
            policyId);
    }

    public async Task<FlowData?> SimulateFlowAsync(SourceService service, NotificationTopic topic, string? clientId)
    {
        // For simulation, we don't have a specific policy ID
        // We need to determine the channel from existing policies
        var existingPolicy = await _dbContext.RoutingPolicies
            .Where(p => p.Service == service && p.Topic == topic && p.IsEnabled)
            .Where(p => clientId == null ? p.ClientId == null : p.ClientId == clientId || p.ClientId == null)
            .OrderByDescending(p => p.ClientId != null) // Client-specific first
            .FirstOrDefaultAsync();

        var channel = existingPolicy?.Channel.ToString() ?? "Email";

        return await BuildFlowDataAsync(service, topic, clientId, channel, null);
    }

    private async Task<FlowData> BuildFlowDataAsync(
        SourceService service,
        NotificationTopic topic,
        string? clientId,
        string channel,
        Guid? currentPolicyId)
    {
        // 1. Get topic metadata
        var topicEntity = await _dbContext.Topics
            .FirstOrDefaultAsync(t => t.Service == service && t.TopicName == topic && t.IsActive);

        TopicInfo? topicInfo = topicEntity != null
            ? new TopicInfo
            {
                Service = topicEntity.Service.ToString(),
                TopicName = topicEntity.TopicName.ToString(),
                DisplayName = topicEntity.DisplayName,
                Description = topicEntity.Description,
                TriggerDescription = topicEntity.TriggerDescription,
                PayloadSchema = topicEntity.PayloadSchema,
                DocsUrl = topicEntity.DocsUrl
            }
            : null;

        // 2. Get template mapping
        var mapping = await _mappingRepository.GetMappingAsync(service, topic, clientId);
        TemplateMappingInfo? templateMapping = null;

        if (mapping != null)
        {
            var template = await _templateRepository.GetByIdAsync(mapping.TemplateId);
            if (template != null)
            {
                templateMapping = new TemplateMappingInfo
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    Subject = template.Subject
                };
            }
        }

        // 3. Get all related policies (same service/topic/client/channel)
        var relatedPolicies = await _dbContext.RoutingPolicies
            .Include(p => p.RecipientGroup)
            .Where(p => p.Service == service && p.Topic == topic)
            .Where(p => clientId == null
                ? p.ClientId == null
                : p.ClientId == clientId || p.ClientId == null)
            .Where(p => p.Channel.ToString() == channel)
            .OrderByDescending(p => p.ClientId != null) // Client-specific first
            .ThenBy(p => p.Role)
            .ToListAsync();

        // If client-specific policies exist, filter out default ones
        var clientSpecificPolicies = relatedPolicies.Where(p => p.ClientId == clientId).ToList();
        if (clientSpecificPolicies.Any())
        {
            relatedPolicies = clientSpecificPolicies;
        }
        else
        {
            relatedPolicies = relatedPolicies.Where(p => p.ClientId == null).ToList();
        }

        var flowPolicies = relatedPolicies.Select(p => new FlowPolicyInfo
        {
            Id = p.Id,
            Role = p.Role.ToString(),
            RecipientGroupId = p.RecipientGroupId,
            IsEnabled = p.IsEnabled,
            IsCurrent = currentPolicyId.HasValue && p.Id == currentPolicyId.Value
        }).ToList();

        // 4. Get recipient groups with members
        var recipientGroups = new List<FlowRecipientGroupInfo>();

        foreach (var policy in relatedPolicies)
        {
            if (policy.RecipientGroup == null) continue;

            var members = await _groupService.GetMembersAsync(policy.RecipientGroupId);
            var memberEmails = members.Select(m => m.Email).ToList();

            recipientGroups.Add(new FlowRecipientGroupInfo
            {
                Id = policy.RecipientGroupId,
                Name = policy.RecipientGroup.Name,
                Role = policy.Role.ToString(),
                MemberCount = members.Count,
                Members = memberEmails
            });
        }

        return new FlowData
        {
            Topic = topicInfo,
            TemplateMapping = templateMapping,
            Channel = channel,
            Policies = flowPolicies,
            RecipientGroups = recipientGroups
        };
    }
}
