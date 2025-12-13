using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for email template data access
/// </summary>
public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly NotificationDbContext _context;

    public EmailTemplateRepository(NotificationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default)
    {
        return await _context.EmailTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<List<EmailTemplate>> GetAllTemplatesAsync(CancellationToken ct = default)
    {
        return await _context.EmailTemplates
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be empty", nameof(name));

        return await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == name, ct);
    }

    public async Task<EmailTemplate?> GetByTypeAsync(string templateType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateType))
            throw new ArgumentException("Template type cannot be empty", nameof(templateType));

        return await _context.EmailTemplates
            .Where(t => t.TemplateType == templateType && t.IsActive)
            .OrderBy(t => t.Name)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<EmailTemplate>> GetAllByTypeAsync(string templateType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateType))
            throw new ArgumentException("Template type cannot be empty", nameof(templateType));

        return await _context.EmailTemplates
            .Where(t => t.TemplateType == templateType)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (string.IsNullOrWhiteSpace(template.Name))
            throw new ArgumentException("Template name is required");

        // Check for duplicate name
        var existing = await _context.EmailTemplates
            .AnyAsync(t => t.Name == template.Name, ct);

        if (existing)
            throw new InvalidOperationException($"A template with name '{template.Name}' already exists");

        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync(ct);

        return template;
    }

    public async Task<EmailTemplate> UpdateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        var existing = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == template.Id, ct);

        if (existing == null)
            throw new InvalidOperationException($"Template with ID {template.Id} not found");

        // Check for name conflict with other templates
        var nameConflict = await _context.EmailTemplates
            .AnyAsync(t => t.Name == template.Name && t.Id != template.Id, ct);

        if (nameConflict)
            throw new InvalidOperationException($"A template with name '{template.Name}' already exists");

        // Update properties
        existing.Name = template.Name;
        existing.Description = template.Description;
        existing.Subject = template.Subject;
        existing.HtmlContent = template.HtmlContent;
        existing.TextContent = template.TextContent;
        existing.Variables = template.Variables;
        existing.TestData = template.TestData;
        existing.DefaultRecipients = template.DefaultRecipients;
        existing.TemplateType = template.TemplateType;
        existing.IsActive = template.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template == null)
            return false;

        _context.EmailTemplates.Remove(template);
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return await _context.EmailTemplates
            .AnyAsync(t => t.Name == name, ct);
    }
}
