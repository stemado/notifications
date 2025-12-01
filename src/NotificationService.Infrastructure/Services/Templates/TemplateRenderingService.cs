using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace NotificationService.Infrastructure.Services.Templates;

/// <summary>
/// Implementation of ITemplateRenderingService using Scriban template engine.
/// Provides Liquid/Jinja2-compatible template rendering with variable extraction and validation.
///
/// Uses Liquid mode for Jinja2-like syntax:
/// - {{ variable }} - Variable substitution
/// - {% for item in items %} ... {% endfor %} - Loops
/// - {% if condition %} ... {% endif %} - Conditionals
/// - {{ variable | filter }} - Filters
/// </summary>
public class TemplateRenderingService : ITemplateRenderingService
{
    private readonly ILogger<TemplateRenderingService> _logger;

    public TemplateRenderingService(ILogger<TemplateRenderingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Render a template with provided data.
    /// Missing variables will display as empty strings.
    /// </summary>
    public string RenderTemplate(string templateContent, object data)
    {
        if (string.IsNullOrEmpty(templateContent))
            return string.Empty;

        try
        {
            // Parse the template with Scriban in Liquid mode (Jinja2-compatible syntax)
            var template = Template.Parse(templateContent, lexerOptions: new LexerOptions
            {
                Lang = ScriptLang.Liquid
            });

            // Check for parsing errors
            if (template.HasErrors)
            {
                var errors = string.Join("; ", template.Messages.Select(m => m.Message));
                throw new InvalidOperationException($"Template parsing failed: {errors}");
            }

            // Create context with safe variable handling
            var context = CreateTemplateContext();

            // Convert data to ScriptObject for template context
            var scriptObject = ConvertDataToScriptObject(data);
            context.PushGlobal(scriptObject);

            // Render the template
            var result = template.Render(context);

            _logger.LogDebug("Template rendered successfully. Length: {Length}", result.Length);
            return result;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to render template. Content length: {Length}", templateContent.Length);
            throw new InvalidOperationException($"Template rendering failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extract all variable names from a template.
    /// Equivalent to Python's meta.find_undeclared_variables().
    /// </summary>
    public IEnumerable<string> ExtractVariables(string templateContent)
    {
        if (string.IsNullOrEmpty(templateContent))
            return Enumerable.Empty<string>();

        try
        {
            var template = Template.Parse(templateContent, lexerOptions: new LexerOptions
            {
                Lang = ScriptLang.Liquid
            });

            if (template.HasErrors)
            {
                _logger.LogWarning("Template has parsing errors, variable extraction may be incomplete");
                return Enumerable.Empty<string>();
            }

            // Use Scriban's built-in variable extraction
            var visitor = new VariableExtractionVisitor();
            visitor.Visit(template.Page);

            var variables = visitor.GetVariables()
                .OrderBy(v => v)
                .ToList();

            _logger.LogDebug("Extracted {Count} variables from template", variables.Count);
            return variables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract variables from template");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Validate template syntax without rendering.
    /// </summary>
    public (bool IsValid, string? Error) ValidateTemplate(string templateContent)
    {
        if (string.IsNullOrEmpty(templateContent))
            return (true, null);

        try
        {
            var template = Template.Parse(templateContent, lexerOptions: new LexerOptions
            {
                Lang = ScriptLang.Liquid
            });

            if (template.HasErrors)
            {
                var errors = string.Join("; ", template.Messages.Select(m => m.Message));
                return (false, errors);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Render template asynchronously (though actual rendering is synchronous in Scriban).
    /// This method provides async interface for compatibility.
    /// </summary>
    public async Task<string> RenderTemplateAsync(
        string templateContent,
        object data,
        CancellationToken ct = default)
    {
        return await Task.Run(
            () => RenderTemplate(templateContent, data),
            ct);
    }

    /// <summary>
    /// Provide template syntax guide for users.
    /// </summary>
    public string GetTemplateSyntaxGuide()
    {
        return @"
# Liquid/Jinja2 Template Syntax Guide

## Basic Variable Substitution
{{ variable }}                    # Inserts the value of variable
{{ ClientName }}                 # Example: Acme Corporation
{{ ImportDate }}                 # Example: 2025-11-03

## Accessing Properties
{{ user.FirstName }}             # Access object properties
{{ items[0].Name }}              # Access array elements

## Filters
{{ value | upcase }}             # Convert to uppercase
{{ value | downcase }}           # Convert to lowercase
{{ value | capitalize }}         # Capitalize first letter
{{ items | size }}               # Get array size
{{ text | truncate: 100 }}       # Truncate text to 100 chars

## Loops
{% for item in items %}
  <li>{{ item.Name }}</li>
{% endfor %}

## Conditionals
{% if condition %}
  <p>This shows when condition is true</p>
{% endif %}

{% if user.Age >= 18 %}
  <p>Adult</p>
{% else %}
  <p>Minor</p>
{% endif %}

{% if status == 'Success' %}
  <span style='color: green;'>Success</span>
{% elsif status == 'Failed' %}
  <span style='color: red;'>Failed</span>
{% else %}
  <span style='color: orange;'>Pending</span>
{% endif %}

## Comparisons
{% if name == 'John' %}
{% if age > 18 %}
{% if value != null %}
{% if items.size > 0 %}

## Example: Email Template
<h1>Hello {{ ClientName }}!</h1>
<p>Your import on {{ ImportDate }} processed {{ RecordsProcessed }} records.</p>

{% if ErrorCount > 0 %}
<p>Warning: {{ ErrorCount }} records failed to import.</p>
{% endif %}

<h2>Import Summary:</h2>
<table>
{% for item in ImportSummaries %}
  <tr>
    <td>{{ item.ImportType }}</td>
    <td>{{ item.TotalCount }}</td>
    <td>{{ item.Status }}</td>
  </tr>
{% endfor %}
</table>

## Notes
- Variable names are case-sensitive
- Use {% elsif %} for else-if conditions (not elif)
- Missing variables display as empty string
- Comments: {% comment %} This is a comment {% endcomment %}
";
    }

    // ==================== Private Helpers ====================

    private static TemplateContext CreateTemplateContext()
    {
        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name,
            StrictVariables = false, // Allow undefined variables - they show as empty
            RecursiveLimit = 1000,
            LoopLimit = 10000
        };

        context.PushCulture(CultureInfo.InvariantCulture);

        return context;
    }

    private ScriptObject ConvertDataToScriptObject(object data)
    {
        var scriptObject = new ScriptObject();

        if (data == null)
            return scriptObject;

        // Handle JSON string input
        if (data is string jsonString && !string.IsNullOrWhiteSpace(jsonString))
        {
            try
            {
                var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (jsonData != null)
                {
                    foreach (var kvp in jsonData)
                    {
                        scriptObject.Add(kvp.Key, ConvertJsonValue(kvp.Value));
                    }
                }
                return scriptObject;
            }
            catch
            {
                // If not valid JSON, treat as simple string value
                scriptObject.Add("value", data);
                return scriptObject;
            }
        }

        // Handle Dictionary input
        if (data is Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                scriptObject.Add(kvp.Key, ConvertJsonValue(kvp.Value));
            }
            return scriptObject;
        }

        // Handle plain objects via reflection
        try
        {
            scriptObject.Import(data);
        }
        catch
        {
            _logger.LogWarning("Failed to import data object type {Type}", data.GetType().Name);
        }

        return scriptObject;
    }

    private static object? ConvertJsonValue(object? value)
    {
        if (value == null)
            return null;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(item => ConvertJsonValue(item))
                    .ToList(),

                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value)),

                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,

                _ => element.ToString()
            };
        }

        return value;
    }
}

/// <summary>
/// Internal visitor class for extracting variables from Scriban AST.
/// Walks the template syntax tree to find all variable references.
/// </summary>
internal class VariableExtractionVisitor
{
    private readonly HashSet<string> _variables = new(StringComparer.Ordinal);
    private readonly Regex _varPattern = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    public List<string> GetVariables() => _variables.OrderBy(v => v).ToList();

    /// <summary>
    /// Recursively visit a script node and all its children to extract variable names.
    /// </summary>
    public void Visit(ScriptNode? node)
    {
        if (node == null) return;

        // Capture variable references from ScriptVariableGlobal nodes
        if (node is ScriptVariableGlobal globalVar)
        {
            if (_varPattern.IsMatch(globalVar.Name))
                _variables.Add(globalVar.Name);
        }
        // Capture variable references from ScriptVariable nodes (covers ScriptVariableLocal)
        else if (node is ScriptVariable scriptVar)
        {
            if (_varPattern.IsMatch(scriptVar.Name))
                _variables.Add(scriptVar.Name);
        }

        // Recursively visit all child nodes using the Children property
        foreach (var child in node.Children)
        {
            Visit(child);
        }
    }
}
