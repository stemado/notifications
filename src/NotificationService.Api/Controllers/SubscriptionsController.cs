using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models.Preferences;
using NotificationService.Infrastructure.Services;
using System.Security.Claims;

namespace NotificationService.Api.Controllers;

/// <summary>
/// API controller for notification subscriptions (Phase 2)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all subscriptions for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationSubscription>>> GetSubscriptions()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId.Value);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions for user {UserId}", userId);
            return StatusCode(500, "Error retrieving subscriptions");
        }
    }

    /// <summary>
    /// Creates or updates a subscription
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<NotificationSubscription>> Subscribe([FromBody] SubscribeRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            var subscription = await _subscriptionService.SubscribeAsync(
                userId.Value,
                request.ClientId,
                request.SagaId,
                request.MinSeverity);

            _logger.LogInformation(
                "User {UserId} subscribed to ClientId={ClientId}, SagaId={SagaId}",
                userId, request.ClientId, request.SagaId);

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
            return StatusCode(500, "Error creating subscription");
        }
    }

    /// <summary>
    /// Deletes a subscription
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult> Unsubscribe([FromQuery] Guid? clientId, [FromQuery] Guid? sagaId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        try
        {
            await _subscriptionService.UnsubscribeAsync(userId.Value, clientId, sagaId);
            _logger.LogInformation(
                "User {UserId} unsubscribed from ClientId={ClientId}, SagaId={SagaId}",
                userId, clientId, sagaId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription for user {UserId}", userId);
            return StatusCode(500, "Error deleting subscription");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Request model for subscribing
/// </summary>
public class SubscribeRequest
{
    public Guid? ClientId { get; set; }
    public Guid? SagaId { get; set; }
    public NotificationSeverity MinSeverity { get; set; }
}
