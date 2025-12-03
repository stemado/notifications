namespace NotificationService.Domain.Enums;

/// <summary>
/// Status of a notification delivery attempt
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Delivery is queued and waiting to be sent
    /// </summary>
    Pending,

    /// <summary>
    /// Delivery is currently being processed
    /// </summary>
    Processing,

    /// <summary>
    /// Delivery was successful
    /// </summary>
    Delivered,

    /// <summary>
    /// Delivery failed and will be retried
    /// </summary>
    Failed,

    /// <summary>
    /// Delivery bounced (email specific)
    /// </summary>
    Bounced,

    /// <summary>
    /// Delivery was cancelled
    /// </summary>
    Cancelled
}
