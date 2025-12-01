using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// Repository interface for email template data access operations
/// </summary>
public interface IEmailTemplateRepository
{
    /// <summary>
    /// Get all active templates sorted by name
    /// </summary>
    Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all templates including inactive ones, sorted by name
    /// </summary>
    Task<List<EmailTemplate>> GetAllTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Get a template by unique name
    /// </summary>
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Create a new template
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a template with the same name already exists</exception>
    Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default);

    /// <summary>
    /// Update an existing template
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the template doesn't exist or name conflicts with another template</exception>
    Task<EmailTemplate> UpdateAsync(EmailTemplate template, CancellationToken ct = default);

    /// <summary>
    /// Delete a template by ID
    /// </summary>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Check if a template with the given name exists
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
