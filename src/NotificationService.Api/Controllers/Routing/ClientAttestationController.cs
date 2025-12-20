using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for client attestation template management
/// </summary>
[ApiController]
[Route("api/client-attestation")]
public class ClientAttestationController : ControllerBase
{
    private readonly IClientAttestationService _attestationService;
    private readonly ILogger<ClientAttestationController> _logger;

    public ClientAttestationController(
        IClientAttestationService attestationService,
        ILogger<ClientAttestationController> logger)
    {
        _attestationService = attestationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available attestation templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<AttestationTemplatesResponse>> GetAttestationTemplates(
        CancellationToken ct)
    {
        var templates = await _attestationService.GetAttestationTemplatesAsync(ct);

        return Ok(new AttestationTemplatesResponse
        {
            Count = templates.Count,
            Templates = templates
        });
    }

    /// <summary>
    /// Get complete attestation configuration for a client
    /// </summary>
    [HttpGet("clients/{clientId}")]
    public async Task<ActionResult<ClientAttestationConfigurationDto>> GetClientConfiguration(
        string clientId,
        CancellationToken ct)
    {
        var config = await _attestationService.GetClientConfigurationAsync(clientId, ct);
        return Ok(config);
    }

    /// <summary>
    /// Enable or update a template for a client
    /// </summary>
    [HttpPut("clients/{clientId}/templates/{templateId:int}")]
    public async Task<ActionResult<ClientAttestationTemplateDto>> EnableTemplate(
        string clientId,
        int templateId,
        [FromBody] EnableTemplateRequest request,
        CancellationToken ct)
    {
        var updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        var result = await _attestationService.EnableTemplateAsync(
            clientId, templateId, request, updatedBy, ct);

        _logger.LogInformation(
            "Updated attestation template {TemplateId} for client {ClientId} via API: enabled={IsEnabled}",
            templateId, clientId, request.IsEnabled);

        return Ok(result);
    }

    /// <summary>
    /// Disable/remove a template from a client
    /// </summary>
    [HttpDelete("clients/{clientId}/templates/{templateId:int}")]
    public async Task<ActionResult> DisableTemplate(
        string clientId,
        int templateId,
        CancellationToken ct)
    {
        await _attestationService.DisableTemplateAsync(clientId, templateId, ct);

        _logger.LogInformation(
            "Disabled attestation template {TemplateId} for client {ClientId} via API",
            templateId, clientId);

        return NoContent();
    }

    /// <summary>
    /// Toggle a template's enabled state for a client
    /// </summary>
    [HttpPost("clients/{clientId}/templates/{templateId:int}/toggle")]
    public async Task<ActionResult<ClientAttestationTemplateDto>> ToggleTemplate(
        string clientId,
        int templateId,
        CancellationToken ct)
    {
        var updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        var result = await _attestationService.ToggleTemplateAsync(
            clientId, templateId, updatedBy, ct);

        _logger.LogInformation(
            "Toggled attestation template {TemplateId} for client {ClientId} via API: enabled={IsEnabled}",
            templateId, clientId, result.IsEnabled);

        return Ok(result);
    }

    /// <summary>
    /// Add a routing policy to a client's template
    /// </summary>
    [HttpPost("clients/{clientId}/templates/{templateId:int}/policies")]
    public async Task<ActionResult<AssignedPolicyDto>> AddPolicy(
        string clientId,
        int templateId,
        [FromBody] AddPolicyRequest request,
        CancellationToken ct)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        var result = await _attestationService.AddPolicyAsync(
            clientId, templateId, request.RoutingPolicyId, createdBy, ct);

        _logger.LogInformation(
            "Added policy {PolicyId} to attestation template {TemplateId} for client {ClientId} via API",
            request.RoutingPolicyId, templateId, clientId);

        return Ok(result);
    }

    /// <summary>
    /// Remove a routing policy from a client's template
    /// </summary>
    [HttpDelete("clients/{clientId}/templates/{templateId:int}/policies/{policyId:guid}")]
    public async Task<ActionResult> RemovePolicy(
        string clientId,
        int templateId,
        Guid policyId,
        CancellationToken ct)
    {
        await _attestationService.RemovePolicyAsync(clientId, templateId, policyId, ct);

        _logger.LogInformation(
            "Removed policy {PolicyId} from attestation template {TemplateId} for client {ClientId} via API",
            policyId, templateId, clientId);

        return NoContent();
    }

    /// <summary>
    /// Add a recipient group to a client's template
    /// </summary>
    [HttpPost("clients/{clientId}/templates/{templateId:int}/groups")]
    public async Task<ActionResult<AssignedGroupDto>> AddGroup(
        string clientId,
        int templateId,
        [FromBody] AddGroupRequest request,
        CancellationToken ct)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "api";

        var result = await _attestationService.AddGroupAsync(
            clientId, templateId, request.RecipientGroupId, request.Role, createdBy, ct);

        _logger.LogInformation(
            "Added group {GroupId} ({Role}) to attestation template {TemplateId} for client {ClientId} via API",
            request.RecipientGroupId, request.Role, templateId, clientId);

        return Ok(result);
    }

    /// <summary>
    /// Remove a recipient group from a client's template
    /// </summary>
    [HttpDelete("clients/{clientId}/templates/{templateId:int}/groups/{groupId:guid}")]
    public async Task<ActionResult> RemoveGroup(
        string clientId,
        int templateId,
        Guid groupId,
        CancellationToken ct)
    {
        await _attestationService.RemoveGroupAsync(clientId, templateId, groupId, ct);

        _logger.LogInformation(
            "Removed group {GroupId} from attestation template {TemplateId} for client {ClientId} via API",
            groupId, templateId, clientId);

        return NoContent();
    }
}

/// <summary>
/// Response containing available attestation templates
/// </summary>
public record AttestationTemplatesResponse
{
    public int Count { get; init; }
    public List<AttestationTemplateDto> Templates { get; init; } = new();
}

/// <summary>
/// Request to add a routing policy to a template
/// </summary>
public record AddPolicyRequest
{
    public Guid RoutingPolicyId { get; init; }
}

/// <summary>
/// Request to add a recipient group to a template
/// </summary>
public record AddGroupRequest
{
    public Guid RecipientGroupId { get; init; }
    public string Role { get; init; } = "To";
}
