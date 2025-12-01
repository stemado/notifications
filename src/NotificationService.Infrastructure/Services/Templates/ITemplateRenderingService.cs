namespace NotificationService.Infrastructure.Services.Templates;

/// <summary>
/// Service interface for rendering templates using Liquid/Jinja2 syntax.
/// Uses Scriban template engine for parsing and rendering.
/// </summary>
public interface ITemplateRenderingService
{
    /// <summary>
    /// Render a template with provided data.
    /// </summary>
    /// <param name="templateContent">The template string with Liquid syntax</param>
    /// <param name="data">Data object/dictionary to use for variable substitution</param>
    /// <returns>Rendered template output</returns>
    /// <exception cref="InvalidOperationException">Thrown when template parsing or rendering fails</exception>
    string RenderTemplate(string templateContent, object data);

    /// <summary>
    /// Extract all variable names referenced in a template.
    /// </summary>
    /// <param name="templateContent">The template string to analyze</param>
    /// <returns>List of unique variable names found in the template</returns>
    IEnumerable<string> ExtractVariables(string templateContent);

    /// <summary>
    /// Validate template syntax without rendering.
    /// </summary>
    /// <param name="templateContent">The template string to validate</param>
    /// <returns>Tuple of (IsValid, ErrorMessage) - ErrorMessage is null when valid</returns>
    (bool IsValid, string? Error) ValidateTemplate(string templateContent);

    /// <summary>
    /// Render a template asynchronously.
    /// </summary>
    Task<string> RenderTemplateAsync(string templateContent, object data, CancellationToken ct = default);

    /// <summary>
    /// Get syntax guide for template authors.
    /// </summary>
    string GetTemplateSyntaxGuide();
}
