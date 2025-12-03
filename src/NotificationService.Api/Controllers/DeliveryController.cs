using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Services.Delivery;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for delivery management and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryTrackingService _deliveryTracking;
    private readonly ILogger<DeliveryController> _logger;

    public DeliveryController(
        IDeliveryTrackingService deliveryTracking,
        ILogger<DeliveryController> logger)
    {
        _deliveryTracking = deliveryTracking;
        _logger = logger;
    }

    /// <summary>
    /// Get the current delivery queue
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult> GetQueue()
    {
        try
        {
            var queue = await _deliveryTracking.GetQueuedDeliveriesAsync();

            // Map to frontend-expected format
            var response = queue.Select(d => new
            {
                id = d.Id,
                notificationId = d.NotificationId,
                channel = d.Channel.ToString(),
                status = MapDeliveryStatus(d.Status),
                attempts = d.AttemptCount,
                maxAttempts = d.MaxAttempts,
                nextRetryAt = d.NextRetryAt?.ToString("o"),
                lastError = d.ErrorMessage,
                createdAt = d.CreatedAt.ToString("o")
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery queue");
            return StatusCode(500, new { error = "Failed to retrieve delivery queue" });
        }
    }

    /// <summary>
    /// Get delivery history with optional filtering
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult> GetHistory(
        [FromQuery] string? channel = null,
        [FromQuery] string? status = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            NotificationChannel? channelEnum = null;
            if (!string.IsNullOrEmpty(channel) && Enum.TryParse<NotificationChannel>(channel, true, out var c))
                channelEnum = c;

            DeliveryStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DeliveryStatus>(status, true, out var s))
                statusEnum = s;

            DateTime? fromDateTime = null;
            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var from))
                fromDateTime = from;

            DateTime? toDateTime = null;
            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var to))
                toDateTime = to;

            var history = await _deliveryTracking.GetDeliveryHistoryAsync(
                channelEnum, statusEnum, fromDateTime, toDateTime, page, pageSize);

            // Map to frontend-expected format
            var response = history.Select(d => new
            {
                id = d.Id,
                notificationId = d.NotificationId,
                channel = d.Channel.ToString(),
                status = MapDeliveryRecordStatus(d.Status, d.DeliveredAt, d.FailedAt),
                deliveredAt = d.DeliveredAt?.ToString("o"),
                failedAt = d.FailedAt?.ToString("o"),
                errorMessage = d.ErrorMessage,
                responseData = d.ResponseData != null ? System.Text.Json.JsonSerializer.Deserialize<object>(d.ResponseData) : null
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery history");
            return StatusCode(500, new { error = "Failed to retrieve delivery history" });
        }
    }

    /// <summary>
    /// Get overall delivery statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        try
        {
            var stats = await _deliveryTracking.GetDeliveryStatsAsync();

            // Map to frontend-expected format
            var response = new
            {
                totalDelivered = stats.TotalDelivered,
                totalFailed = stats.TotalFailed,
                totalPending = stats.TotalPending,
                deliveryRatePercent = stats.DeliveryRatePercent,
                avgDeliveryTimeMs = stats.AvgDeliveryTimeMs,
                channelStats = stats.ChannelStats.Select(cs => new
                {
                    channel = cs.Channel.ToString(),
                    delivered = cs.Delivered,
                    failed = cs.Failed,
                    pending = cs.Pending
                })
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery stats");
            return StatusCode(500, new { error = "Failed to retrieve delivery statistics" });
        }
    }

    /// <summary>
    /// Retry a failed delivery
    /// </summary>
    [HttpPost("{id}/retry")]
    public async Task<ActionResult> RetryDelivery(Guid id)
    {
        try
        {
            await _deliveryTracking.RetryDeliveryAsync(id);
            return Ok(new { message = "Delivery queued for retry" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying delivery {DeliveryId}", id);
            return StatusCode(500, new { error = "Failed to retry delivery" });
        }
    }

    /// <summary>
    /// Cancel a pending delivery
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelDelivery(Guid id)
    {
        try
        {
            await _deliveryTracking.CancelDeliveryAsync(id);
            return Ok(new { message = "Delivery cancelled" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling delivery {DeliveryId}", id);
            return StatusCode(500, new { error = "Failed to cancel delivery" });
        }
    }

    private static string MapDeliveryStatus(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending => "pending",
            DeliveryStatus.Processing => "processing",
            DeliveryStatus.Failed => "retry",
            _ => status.ToString().ToLowerInvariant()
        };
    }

    private static string MapDeliveryRecordStatus(DeliveryStatus status, DateTime? deliveredAt, DateTime? failedAt)
    {
        if (status == DeliveryStatus.Delivered && deliveredAt.HasValue)
            return "delivered";
        if (status == DeliveryStatus.Failed && failedAt.HasValue)
            return "failed";
        if (status == DeliveryStatus.Bounced)
            return "bounced";

        return status.ToString().ToLowerInvariant();
    }
}
