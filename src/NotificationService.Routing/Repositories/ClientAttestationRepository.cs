using Microsoft.EntityFrameworkCore;
using NotificationService.Routing.Data;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Repositories;

/// <summary>
/// Repository implementation for ClientAttestationTemplate data access
/// </summary>
public class ClientAttestationRepository : IClientAttestationRepository
{
    private readonly RoutingDbContext _context;

    public ClientAttestationRepository(RoutingDbContext context)
    {
        _context = context;
    }

    public async Task<ClientAttestationTemplate> CreateAsync(ClientAttestationTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        _context.ClientAttestationTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<ClientAttestationTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.ClientAttestationTemplates
            .Include(t => t.Policies)
                .ThenInclude(p => p.RoutingPolicy)
                    .ThenInclude(rp => rp!.RecipientGroup)
            .Include(t => t.Groups)
                .ThenInclude(g => g.RecipientGroup)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<ClientAttestationTemplate?> GetByClientAndTemplateAsync(string clientId, int templateId)
    {
        return await _context.ClientAttestationTemplates
            .Include(t => t.Policies)
                .ThenInclude(p => p.RoutingPolicy)
                    .ThenInclude(rp => rp!.RecipientGroup)
            .Include(t => t.Groups)
                .ThenInclude(g => g.RecipientGroup)
            .FirstOrDefaultAsync(t => t.ClientId == clientId && t.TemplateId == templateId);
    }

    public async Task<List<ClientAttestationTemplate>> GetByClientAsync(string clientId, bool includeDisabled = false)
    {
        var query = _context.ClientAttestationTemplates
            .Where(t => t.ClientId == clientId);

        if (!includeDisabled)
        {
            query = query.Where(t => t.IsEnabled);
        }

        return await query
            .Include(t => t.Policies)
                .ThenInclude(p => p.RoutingPolicy)
                    .ThenInclude(rp => rp!.RecipientGroup)
            .Include(t => t.Groups)
                .ThenInclude(g => g.RecipientGroup)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.TemplateId)
            .ToListAsync();
    }

    public async Task<ClientAttestationTemplate> UpdateAsync(ClientAttestationTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _context.ClientAttestationTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await _context.ClientAttestationTemplates.FindAsync(id)
            ?? throw new InvalidOperationException($"Client attestation template {id} not found");

        _context.ClientAttestationTemplates.Remove(template);
        await _context.SaveChangesAsync();
    }

    public async Task<ClientAttestationTemplate> ToggleAsync(Guid id)
    {
        var template = await _context.ClientAttestationTemplates.FindAsync(id)
            ?? throw new InvalidOperationException($"Client attestation template {id} not found");

        template.IsEnabled = !template.IsEnabled;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return template;
    }

    // Policy assignment operations
    public async Task<ClientAttestationTemplatePolicy> AddPolicyAsync(
        Guid clientAttestationTemplateId,
        Guid routingPolicyId,
        string? createdBy = null)
    {
        var policy = new ClientAttestationTemplatePolicy
        {
            ClientAttestationTemplateId = clientAttestationTemplateId,
            RoutingPolicyId = routingPolicyId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.ClientAttestationTemplatePolicies.Add(policy);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return await _context.ClientAttestationTemplatePolicies
            .Include(p => p.RoutingPolicy)
                .ThenInclude(rp => rp!.RecipientGroup)
            .FirstAsync(p => p.Id == policy.Id);
    }

    public async Task RemovePolicyAsync(Guid clientAttestationTemplateId, Guid routingPolicyId)
    {
        var policy = await _context.ClientAttestationTemplatePolicies
            .FirstOrDefaultAsync(p =>
                p.ClientAttestationTemplateId == clientAttestationTemplateId &&
                p.RoutingPolicyId == routingPolicyId)
            ?? throw new InvalidOperationException(
                $"Policy assignment not found for template {clientAttestationTemplateId} and policy {routingPolicyId}");

        _context.ClientAttestationTemplatePolicies.Remove(policy);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ClientAttestationTemplatePolicy>> GetPoliciesAsync(Guid clientAttestationTemplateId)
    {
        return await _context.ClientAttestationTemplatePolicies
            .Where(p => p.ClientAttestationTemplateId == clientAttestationTemplateId)
            .Include(p => p.RoutingPolicy)
                .ThenInclude(rp => rp!.RecipientGroup)
            .ToListAsync();
    }

    // Group assignment operations
    public async Task<ClientAttestationTemplateGroup> AddGroupAsync(
        Guid clientAttestationTemplateId,
        Guid recipientGroupId,
        string role,
        string? createdBy = null)
    {
        if (!Enum.TryParse<DeliveryRole>(role, true, out var deliveryRole))
        {
            throw new ArgumentException($"Invalid role: {role}. Must be To, Cc, or Bcc.");
        }

        var group = new ClientAttestationTemplateGroup
        {
            ClientAttestationTemplateId = clientAttestationTemplateId,
            RecipientGroupId = recipientGroupId,
            Role = deliveryRole,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.ClientAttestationTemplateGroups.Add(group);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return await _context.ClientAttestationTemplateGroups
            .Include(g => g.RecipientGroup)
                .ThenInclude(rg => rg!.Memberships)
            .FirstAsync(g => g.Id == group.Id);
    }

    public async Task RemoveGroupAsync(Guid clientAttestationTemplateId, Guid recipientGroupId)
    {
        var group = await _context.ClientAttestationTemplateGroups
            .FirstOrDefaultAsync(g =>
                g.ClientAttestationTemplateId == clientAttestationTemplateId &&
                g.RecipientGroupId == recipientGroupId)
            ?? throw new InvalidOperationException(
                $"Group assignment not found for template {clientAttestationTemplateId} and group {recipientGroupId}");

        _context.ClientAttestationTemplateGroups.Remove(group);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ClientAttestationTemplateGroup>> GetGroupsAsync(Guid clientAttestationTemplateId)
    {
        return await _context.ClientAttestationTemplateGroups
            .Where(g => g.ClientAttestationTemplateId == clientAttestationTemplateId)
            .Include(g => g.RecipientGroup)
                .ThenInclude(rg => rg!.Memberships)
            .ToListAsync();
    }
}
