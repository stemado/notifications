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

    /// <summary>
    /// Fix stale delivery records that have DeliveredAt set but Status is still Pending.
    /// This is a one-time fix for records created before the bug was fixed.
    /// </summary>
    [HttpPost("fix-stale")]
    public async Task<ActionResult> FixStaleDeliveries()
    {
        try
        {
            var fixedCount = await _deliveryTracking.FixStaleDeliveredRecordsAsync();
            return Ok(new
            {
                message = fixedCount > 0
                    ? $"Fixed {fixedCount} stale delivery records"
                    : "No stale records found to fix",
                fixedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing stale deliveries");
            return StatusCode(500, new { error = "Failed to fix stale deliveries" });
        }
    }

    /// <summary>
    /// Delete old delivered records to clean up the database.
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult> CleanupOldDeliveries([FromQuery] int daysOld = 30)
    {
        try
        {
            var deletedCount = await _deliveryTracking.DeleteOldDeliveredAsync(daysOld);
            return Ok(new
            {
                message = deletedCount > 0
                    ? $"Deleted {deletedCount} old delivered records older than {daysOld} days"
                    : "No old records found to delete",
                deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old deliveries");
            return StatusCode(500, new { error = "Failed to cleanup old deliveries" });
        }
    }

    /// <summary>
    /// Get all delivery records and queue items for a specific notification.
    /// Used for the delivery timeline view in the modal.
    /// </summary>
    [HttpGet("by-notification/{notificationId}")]
    public async Task<ActionResult> GetByNotification(Guid notificationId)
    {
        try
        {
            var result = await _deliveryTracking.GetDeliveriesByNotificationAsync(notificationId);

            var response = new
            {
                notificationId = result.NotificationId,
                deliveries = result.Deliveries.Select(d => new
                {
                    id = d.Id,
                    notificationId = d.NotificationId,
                    channel = d.Channel.ToString(),
                    status = MapDeliveryRecordStatus(d.Status, d.DeliveredAt, d.FailedAt),
                    deliveredAt = d.DeliveredAt?.ToString("o"),
                    failedAt = d.FailedAt?.ToString("o"),
                    errorMessage = d.ErrorMessage,
                    responseData = d.ResponseData != null
                        ? System.Text.Json.JsonSerializer.Deserialize<object>(d.ResponseData)
                        : null
                }),
                queuedItems = result.QueuedItems.Select(d => new
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
                })
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deliveries for notification {NotificationId}", notificationId);
            return StatusCode(500, new { error = "Failed to retrieve deliveries for notification" });
        }
    }

    /// <summary>
    /// Re-queue a notification for delivery.
    /// Creates new delivery records for failed channels.
    /// </summary>
    [HttpPost("requeue/{notificationId}")]
    public async Task<ActionResult> RequeueNotification(Guid notificationId)
    {
        try
        {
            await _deliveryTracking.RequeueNotificationAsync(notificationId);
            return Ok(new { message = "Notification queued for re-delivery" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requeueing notification {NotificationId}", notificationId);
            return StatusCode(500, new { error = "Failed to requeue notification" });
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
