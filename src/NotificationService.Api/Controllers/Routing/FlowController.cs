using Microsoft.AspNetCore.Mvc;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Services;

namespace NotificationService.Api.Controllers.Routing;

/// <summary>
/// API controller for notification flow visualization.
/// Provides endpoints to get flow data for policies and simulate routing.
/// </summary>
[ApiController]
[Route("api/routing/flow")]
public class FlowController : ControllerBase
{
    private readonly IFlowVisualizationService _flowService;
    private readonly ILogger<FlowController> _logger;

    public FlowController(
        IFlowVisualizationService flowService,
        ILogger<FlowController> logger)
    {
        _flowService = flowService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete flow data for a specific policy.
    /// Returns topic metadata, template mapping, related policies, and recipient groups.
    /// </summary>
    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<FlowData>> GetFlowForPolicy(Guid policyId)
    {
        var flowData = await _flowService.GetFlowForPolicyAsync(policyId);
        if (flowData == null)
        {
            return NotFound($"Policy {policyId} not found");
        }

        _logger.LogDebug("Retrieved flow data for policy {PolicyId}", policyId);
        return Ok(flowData);
    }

    /// <summary>
    /// Simulate notification routing for a given service/topic/client combination.
    /// Returns what would happen if a notification was sent without actually sending.
    /// </summary>
    [HttpPost("simulate")]
    public async Task<ActionResult<FlowData>> SimulateFlow([FromBody] SimulateFlowRequest request)
    {
        if (!Enum.TryParse<SourceService>(request.Service, out var service))
        {
            return BadRequest($"Invalid service: {request.Service}");
        }

        if (!Enum.TryParse<NotificationTopic>(request.TopicName, out var topic))
        {
            return BadRequest($"Invalid topic: {request.TopicName}");
        }

        var flowData = await _flowService.SimulateFlowAsync(service, topic, request.ClientId);
        if (flowData == null)
        {
            return NotFound("No routing configuration found for the specified service/topic");
        }

        _logger.LogDebug(
            "Simulated flow for {Service}/{Topic} (clientId: {ClientId})",
            request.Service, request.TopicName, request.ClientId ?? "(default)");

        return Ok(flowData);
    }
}
