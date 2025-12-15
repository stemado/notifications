using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services.Templates;
using NotificationService.Infrastructure.Services.Email;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Email template management API controller.
/// Provides routes for managing email templates, previewing renders, and sending templated emails.
/// </summary>
[ApiController]
[Route("api/templates")]
[Produces("application/json")]
public class TemplatesController : ControllerBase
{
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly ITemplateRenderingService _renderingService;
    private readonly IEmailService _emailService;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<TemplatesController> _logger;

    // System user ID for ad-hoc/test emails sent via API
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public TemplatesController(
        IEmailTemplateRepository templateRepository,
        ITemplateRenderingService renderingService,
        IEmailService emailService,
        INotificationDeliveryRepository deliveryRepository,
        INotificationRepository notificationRepository,
        ILogger<TemplatesController> logger)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _deliveryRepository = deliveryRepository ?? throw new ArgumentNullException(nameof(deliveryRepository));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== Template Management Endpoints ====================

    /// <summary>
    /// GET /api/templates - Get all active email templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TemplateListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTemplates(CancellationToken ct)
    {
        var templates = await _templateRepository.GetActiveTemplatesAsync(ct);

        var response = templates.Select(t => new EmailTemplateDto(
            Id: t.Id,
            Name: t.Name,
            Description: t.Description,
            Subject: t.Subject,
            TemplateType: t.TemplateType,
            IsActive: t.IsActive,
            CreatedAt: t.CreatedAt,
            UpdatedAt: t.UpdatedAt
        )).ToList();

        _logger.LogInformation("Retrieved {Count} active email templates", templates.Count);
        return Ok(new TemplateListResponse(response.Count, response));
    }

    /// <summary>
    /// GET /api/templates/all - Get all email templates including inactive
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(TemplateListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTemplates(CancellationToken ct)
    {
        var templates = await _templateRepository.GetAllTemplatesAsync(ct);

        var response = templates.Select(t => new EmailTemplateDto(
            Id: t.Id,
            Name: t.Name,
            Description: t.Description,
            Subject: t.Subject,
            TemplateType: t.TemplateType,
            IsActive: t.IsActive,
            CreatedAt: t.CreatedAt,
            UpdatedAt: t.UpdatedAt
        )).ToList();

        _logger.LogInformation("Retrieved {Count} total email templates", templates.Count);
        return Ok(new TemplateListResponse(response.Count, response));
    }

    /// <summary>
    /// GET /api/templates/{id} - Get a specific template by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateById(int id, CancellationToken ct)
    {
        var template = await _templateRepository.GetByIdAsync(id, ct);
        if (template == null)
        {
            _logger.LogWarning("Template not found: {TemplateId}", id);
            return NotFound(new { message = $"Template with ID {id} not found" });
        }

        var response = MapToDetailDto(template);
        _logger.LogInformation("Retrieved template: {TemplateId}", id);
        return Ok(response);
    }

    /// <summary>
    /// GET /api/templates/name/{name} - Get a template by name
    /// </summary>
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateByName(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Template name is required" });

        var template = await _templateRepository.GetByNameAsync(name, ct);
        if (template == null)
        {
            _logger.LogWarning("Template not found: {TemplateName}", name);
            return NotFound(new { message = $"Template '{name}' not found" });
        }

        var response = MapToDetailDto(template);
        _logger.LogInformation("Retrieved template by name: {TemplateName}", name);
        return Ok(response);
    }

    /// <summary>
    /// GET /api/templates/type/{type} - Get a template by template type (e.g., workflow_triggered, file_detected)
    /// </summary>
    /// <remarks>
    /// Returns the first active template matching the specified type.
    /// Template types are used to categorize templates by their purpose (workflow_triggered, file_detected, etc.)
    /// </remarks>
    [HttpGet("type/{type}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateByType(string type, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(type))
            return BadRequest(new { message = "Template type is required" });

        var template = await _templateRepository.GetByTypeAsync(type, ct);
        if (template == null)
        {
            _logger.LogWarning("No active template found for type: {TemplateType}", type);
            return NotFound(new { message = $"No active template found for type '{type}'" });
        }

        var response = MapToDetailDto(template);
        _logger.LogInformation("Retrieved template by type: {TemplateType}, Name: {TemplateName}", type, template.Name);
        return Ok(response);
    }

    /// <summary>
    /// GET /api/templates/type/{type}/all - Get all templates of a specific type
    /// </summary>
    [HttpGet("type/{type}/all")]
    [ProducesResponseType(typeof(TemplateListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTemplatesByType(string type, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(type))
            return BadRequest(new { message = "Template type is required" });

        var templates = await _templateRepository.GetAllByTypeAsync(type, ct);
        var response = templates.Select(t => new EmailTemplateDto(
            Id: t.Id,
            Name: t.Name,
            Description: t.Description,
            Subject: t.Subject,
            TemplateType: t.TemplateType,
            IsActive: t.IsActive,
            CreatedAt: t.CreatedAt,
            UpdatedAt: t.UpdatedAt
        )).ToList();

        _logger.LogInformation("Retrieved {Count} templates for type: {TemplateType}", templates.Count, type);
        return Ok(new TemplateListResponse(response.Count, response));
    }

    // ==================== Template CRUD Endpoints ====================

    /// <summary>
    /// POST /api/templates - Create a new email template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateEmailTemplateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Template name is required" });

        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { message = "Subject is required" });

        try
        {
            var template = new EmailTemplate
            {
                Name = request.Name,
                Description = request.Description,
                Subject = request.Subject,
                HtmlContent = request.HtmlContent,
                TextContent = request.TextContent,
                Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null,
                TestData = request.TestData != null ? JsonSerializer.Serialize(request.TestData) : null,
                DefaultRecipients = !string.IsNullOrWhiteSpace(request.DefaultRecipients) ? request.DefaultRecipients : null,
                TemplateType = request.TemplateType ?? "notification",
                IsActive = request.IsActive ?? true
            };

            var created = await _templateRepository.CreateAsync(template, ct);
            var response = MapToDetailDto(created);

            _logger.LogInformation("Created email template: {TemplateId} - {TemplateName}", created.Id, created.Name);
            return CreatedAtAction(nameof(GetTemplateById), new { id = created.Id }, response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Template creation failed - duplicate name: {TemplateName}", request.Name);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/templates/{id} - Update an existing email template
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EmailTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateEmailTemplateRequest request, CancellationToken ct)
    {
        var existing = await _templateRepository.GetByIdAsync(id, ct);
        if (existing == null)
        {
            _logger.LogWarning("Template not found for update: {TemplateId}", id);
            return NotFound(new { message = $"Template with ID {id} not found" });
        }

        try
        {
            var template = new EmailTemplate
            {
                Id = id,
                Name = request.Name ?? existing.Name,
                Description = request.Description ?? existing.Description,
                Subject = request.Subject ?? existing.Subject,
                HtmlContent = request.HtmlContent ?? existing.HtmlContent,
                TextContent = request.TextContent ?? existing.TextContent,
                Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : existing.Variables,
                TestData = request.TestData != null ? JsonSerializer.Serialize(request.TestData) : existing.TestData,
                DefaultRecipients = request.DefaultRecipients != null
                    ? (!string.IsNullOrWhiteSpace(request.DefaultRecipients) ? request.DefaultRecipients : null)
                    : existing.DefaultRecipients,
                TemplateType = request.TemplateType ?? existing.TemplateType,
                IsActive = request.IsActive ?? existing.IsActive
            };

            var updated = await _templateRepository.UpdateAsync(template, ct);
            var response = MapToDetailDto(updated);

            _logger.LogInformation("Updated email template: {TemplateId} - {TemplateName}", updated.Id, updated.Name);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Template update failed - duplicate name: {TemplateName}", request.Name);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/templates/{id} - Delete an email template
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(int id, CancellationToken ct)
    {
        var deleted = await _templateRepository.DeleteAsync(id, ct);
        if (!deleted)
        {
            _logger.LogWarning("Template not found for deletion: {TemplateId}", id);
            return NotFound(new { message = $"Template with ID {id} not found" });
        }

        _logger.LogInformation("Deleted email template: {TemplateId}", id);
        return Ok(new { message = $"Template {id} deleted successfully" });
    }

    // ==================== Template Rendering Endpoints ====================

    /// <summary>
    /// POST /api/templates/preview - Preview template rendering with sample data
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(PreviewTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewTemplate([FromBody] PreviewTemplateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName))
            return BadRequest(new { message = "Template name is required" });

        var template = await _templateRepository.GetByNameAsync(request.TemplateName, ct);
        if (template == null)
            return NotFound(new { message = $"Template '{request.TemplateName}' not found" });

        // Parse custom data
        var data = string.IsNullOrEmpty(request.Data)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(request.Data)
              ?? new Dictionary<string, object>();

        // Render subject and body
        var renderedSubject = _renderingService.RenderTemplate(template.Subject, data);
        var renderedBody = _renderingService.RenderTemplate(template.HtmlContent ?? string.Empty, data);

        _logger.LogInformation("Previewed template: {TemplateName}", request.TemplateName);

        return Ok(new PreviewTemplateResponse(
            TemplateName: request.TemplateName,
            RenderedSubject: renderedSubject,
            RenderedBody: renderedBody,
            PreviewedAt: DateTime.UtcNow
        ));
    }

    /// <summary>
    /// POST /api/templates/extract-variables/{name} - Extract variables from a template
    /// </summary>
    [HttpPost("extract-variables/{name}")]
    [ProducesResponseType(typeof(ExtractVariablesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExtractTemplateVariables(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Template name is required" });

        var template = await _templateRepository.GetByNameAsync(name, ct);
        if (template == null)
            return NotFound(new { message = $"Template '{name}' not found" });

        // Extract from subject and body
        var subjectVars = _renderingService.ExtractVariables(template.Subject);
        var bodyVars = _renderingService.ExtractVariables(template.HtmlContent ?? string.Empty);

        // Combine and deduplicate
        var allVariables = new HashSet<string>(subjectVars);
        allVariables.UnionWith(bodyVars);

        var sortedVariables = allVariables.OrderBy(v => v).ToList();

        _logger.LogInformation("Extracted {Count} variables from template: {TemplateName}", sortedVariables.Count, name);

        return Ok(new ExtractVariablesResponse(
            TemplateName: name,
            Variables: sortedVariables,
            VariableCount: sortedVariables.Count
        ));
    }

    /// <summary>
    /// POST /api/templates/validate/{name} - Validate template syntax
    /// </summary>
    [HttpPost("validate/{name}")]
    [ProducesResponseType(typeof(ValidateTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateTemplate(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Template name is required" });

        var template = await _templateRepository.GetByNameAsync(name, ct);
        if (template == null)
            return NotFound(new { message = $"Template '{name}' not found" });

        var (isValidSubject, subjectError) = _renderingService.ValidateTemplate(template.Subject);
        var (isValidBody, bodyError) = _renderingService.ValidateTemplate(template.HtmlContent ?? string.Empty);

        var isValid = isValidSubject && isValidBody;
        var errors = new List<string>();
        if (!isValidSubject) errors.Add($"Subject: {subjectError}");
        if (!isValidBody) errors.Add($"Body: {bodyError}");

        _logger.LogInformation("Validated template: {TemplateName}, Valid: {IsValid}", name, isValid);

        return Ok(new ValidateTemplateResponse(
            TemplateName: name,
            IsValid: isValid,
            Errors: errors,
            ValidatedAt: DateTime.UtcNow
        ));
    }

    // ==================== Email Sending Endpoints ====================

    /// <summary>
    /// POST /api/templates/send - Send an email using a template with delivery tracking
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendTemplatedEmail([FromBody] SendTemplatedEmailRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName))
            return BadRequest(new { message = "Template name is required" });

        if (request.Recipients == null || !request.Recipients.Any())
            return BadRequest(new { message = "At least one recipient is required" });

        var template = await _templateRepository.GetByNameAsync(request.TemplateName, ct);
        if (template == null)
            return NotFound(new { message = $"Template '{request.TemplateName}' not found" });

        // Parse template data
        var templateData = string.IsNullOrEmpty(request.TemplateData)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(request.TemplateData)
              ?? new Dictionary<string, object>();

        _logger.LogInformation(
            "Sending templated email: {TemplateName} to {RecipientCount} recipients via {Provider}",
            request.TemplateName,
            request.Recipients.Count,
            _emailService.CurrentProvider);

        // Render template
        var renderedSubject = _renderingService.RenderTemplate(template.Subject, templateData);
        var renderedBody = _renderingService.RenderTemplate(template.HtmlContent ?? string.Empty, templateData);

        // Create a Notification record for audit trail (ad-hoc/test emails must be tracked)
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = SystemUserId,
            Severity = NotificationSeverity.Info,
            Title = $"Email: {renderedSubject}",
            Message = $"Templated email sent to {string.Join(", ", request.Recipients)} using template '{request.TemplateName}'",
            EventType = "AdHocEmail",
            CreatedAt = DateTime.UtcNow,
            AcknowledgedAt = DateTime.UtcNow, // Auto-acknowledge since this is a send action
            Metadata = new Dictionary<string, object>
            {
                ["templateName"] = request.TemplateName,
                ["recipients"] = request.Recipients,
                ["subject"] = renderedSubject
            }
        };

        await _notificationRepository.CreateAsync(notification);

        // Create delivery record for tracking linked to the notification
        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            Channel = NotificationChannel.Email,
            Status = DeliveryStatus.Processing,
            AttemptCount = 1,
            MaxAttempts = 1,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Send email via email service
            var result = await _emailService.SendEmailAsync(
                request.Recipients,
                renderedSubject,
                renderedBody,
                true, // isHtml
                ct);

            if (result.Success)
            {
                // Update delivery record with success
                delivery.Status = DeliveryStatus.Delivered;
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.ResponseData = JsonSerializer.Serialize(new
                {
                    messageId = result.MessageId,
                    provider = result.Provider,
                    templateName = request.TemplateName,
                    recipients = request.Recipients,
                    subject = renderedSubject
                });

                await _deliveryRepository.CreateAsync(delivery);

                _logger.LogInformation(
                    "Email sent and tracked: {TemplateName}, MessageId: {MessageId}, DeliveryId: {DeliveryId}, Provider: {Provider}",
                    request.TemplateName,
                    result.MessageId,
                    delivery.Id,
                    result.Provider);

                return Ok(new SendEmailResponse(
                    Success: true,
                    MessageId: result.MessageId,
                    DeliveryId: delivery.Id,
                    Message: $"Email sent and tracked via {result.Provider}",
                    SentAt: result.SentAt
                ));
            }

            // Update delivery record with failure
            delivery.Status = DeliveryStatus.Failed;
            delivery.FailedAt = DateTime.UtcNow;
            delivery.ErrorMessage = result.ErrorMessage;
            delivery.ResponseData = JsonSerializer.Serialize(new
            {
                provider = result.Provider,
                templateName = request.TemplateName,
                recipients = request.Recipients,
                error = result.ErrorMessage
            });

            await _deliveryRepository.CreateAsync(delivery);

            _logger.LogWarning(
                "Email send failed and tracked: {TemplateName}, DeliveryId: {DeliveryId}, Provider: {Provider}, Error: {Error}",
                request.TemplateName,
                delivery.Id,
                result.Provider,
                result.ErrorMessage);

            return BadRequest(new SendEmailResponse(
                Success: false,
                DeliveryId: delivery.Id,
                ErrorMessage: result.ErrorMessage,
                Message: $"Email send failed via {result.Provider}: {result.ErrorMessage}"
            ));
        }
        catch (Exception ex)
        {
            // Record exception in delivery tracking
            delivery.Status = DeliveryStatus.Failed;
            delivery.FailedAt = DateTime.UtcNow;
            delivery.ErrorMessage = ex.Message;

            await _deliveryRepository.CreateAsync(delivery);

            _logger.LogError(ex,
                "Email send exception tracked: {TemplateName}, DeliveryId: {DeliveryId}",
                request.TemplateName,
                delivery.Id);

            throw;
        }
    }

    // ==================== Health Check Endpoint ====================

    /// <summary>
    /// GET /api/templates/health - Check email template service health
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(TemplateServiceHealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthStatus(CancellationToken ct)
    {
        try
        {
            // Verify database connectivity by getting template count
            var templates = await _templateRepository.GetAllTemplatesAsync(ct);
            var activeCount = templates.Count(t => t.IsActive);

            return Ok(new TemplateServiceHealthResponse(
                IsHealthy: true,
                Status: "Healthy",
                CheckedAt: DateTime.UtcNow,
                TotalTemplates: templates.Count,
                ActiveTemplates: activeCount
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template service health check failed");
            return Ok(new TemplateServiceHealthResponse(
                IsHealthy: false,
                Status: "Unhealthy",
                CheckedAt: DateTime.UtcNow,
                Error: ex.Message
            ));
        }
    }

    /// <summary>
    /// GET /api/templates/syntax-guide - Get template syntax documentation
    /// </summary>
    [HttpGet("syntax-guide")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetSyntaxGuide()
    {
        return Ok(new { guide = _renderingService.GetTemplateSyntaxGuide() });
    }

    // ==================== Private Helpers ====================

    private static EmailTemplateDetailDto MapToDetailDto(EmailTemplate template)
    {
        return new EmailTemplateDetailDto(
            Id: template.Id,
            Name: template.Name,
            Description: template.Description,
            Subject: template.Subject,
            HtmlContent: template.HtmlContent,
            TextContent: template.TextContent,
            Variables: ParseVariables(template.Variables),
            TestData: template.TestData != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(template.TestData)
                : null,
            DefaultRecipients: template.DefaultRecipients,
            TemplateType: template.TemplateType,
            IsActive: template.IsActive,
            CreatedAt: template.CreatedAt,
            UpdatedAt: template.UpdatedAt
        );
    }

    /// <summary>
    /// Parses variables field which may be stored as either:
    /// - JSON object: {"ClientName": "description", ...} → Dictionary&lt;string, string&gt;
    /// - JSON array: ["ClientName", "ImportDate", ...] → Dictionary with empty descriptions
    /// </summary>
    private static Dictionary<string, string>? ParseVariables(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson))
            return null;

        using var doc = JsonDocument.Parse(variablesJson);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            // Standard format: {"key": "description"}
            return JsonSerializer.Deserialize<Dictionary<string, string>>(variablesJson);
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            // Legacy format: ["key1", "key2"] - convert to dictionary with empty descriptions
            var result = new Dictionary<string, string>();
            foreach (var element in root.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    var key = element.GetString();
                    if (!string.IsNullOrEmpty(key))
                        result[key] = string.Empty;
                }
            }
            return result.Count > 0 ? result : null;
        }

        throw new InvalidOperationException(
            $"Variables field has unexpected JSON format. Expected object or array, got {root.ValueKind}. Value: {variablesJson}");
    }
}

// ==================== Request/Response DTOs ====================

/// <summary>Template list response wrapper</summary>
public record TemplateListResponse(int Count, List<EmailTemplateDto> Templates);

/// <summary>Email template summary</summary>
public record EmailTemplateDto(
    int Id,
    string Name,
    string? Description,
    string Subject,
    string TemplateType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Email template with full details</summary>
public record EmailTemplateDetailDto(
    int Id,
    string Name,
    string? Description,
    string Subject,
    string? HtmlContent,
    string? TextContent,
    Dictionary<string, string>? Variables,
    Dictionary<string, object>? TestData,
    string? DefaultRecipients,
    string TemplateType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Create email template request</summary>
public record CreateEmailTemplateRequest(
    string Name,
    string Subject,
    string? Description = null,
    string? HtmlContent = null,
    string? TextContent = null,
    Dictionary<string, string>? Variables = null,
    Dictionary<string, object>? TestData = null,
    string? DefaultRecipients = null,
    string? TemplateType = null,
    bool? IsActive = null
);

/// <summary>Update email template request</summary>
public record UpdateEmailTemplateRequest(
    string? Name = null,
    string? Description = null,
    string? Subject = null,
    string? HtmlContent = null,
    string? TextContent = null,
    Dictionary<string, string>? Variables = null,
    Dictionary<string, object>? TestData = null,
    string? DefaultRecipients = null,
    string? TemplateType = null,
    bool? IsActive = null
);

/// <summary>Preview template request</summary>
public record PreviewTemplateRequest(
    string TemplateName,
    string? Data = null
);

/// <summary>Preview template response</summary>
public record PreviewTemplateResponse(
    string TemplateName,
    string RenderedSubject,
    string RenderedBody,
    DateTime PreviewedAt
);

/// <summary>Extract variables response</summary>
public record ExtractVariablesResponse(
    string TemplateName,
    List<string> Variables,
    int VariableCount
);

/// <summary>Validate template response</summary>
public record ValidateTemplateResponse(
    string TemplateName,
    bool IsValid,
    List<string> Errors,
    DateTime ValidatedAt
);

/// <summary>Send templated email request</summary>
public record SendTemplatedEmailRequest(
    string TemplateName,
    List<string> Recipients,
    string? TemplateData = null
);

/// <summary>Send email response with delivery tracking</summary>
public record SendEmailResponse(
    bool Success,
    string? MessageId = null,
    Guid? DeliveryId = null,
    string? ErrorMessage = null,
    string? Message = null,
    DateTime? SentAt = null
);

/// <summary>Template service health response</summary>
public record TemplateServiceHealthResponse(
    bool IsHealthy,
    string Status,
    DateTime CheckedAt,
    int? TotalTemplates = null,
    int? ActiveTemplates = null,
    string? Error = null
);
