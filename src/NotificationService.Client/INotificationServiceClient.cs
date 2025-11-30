using NotificationService.Client.Events;
using NotificationService.Client.Models;

namespace NotificationService.Client;

/// <summary>
/// Client interface for consuming the NotificationService API.
/// All methods throw on failure - no silent fallbacks.
/// </summary>
public interface INotificationServiceClient
{
    #region Core Notification Operations

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="request">The notification details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID if successful</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> CreateNotificationAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new notification or updates an existing one with the same GroupKey.
    /// </summary>
    /// <param name="request">The notification details (must include GroupKey for deduplication)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID and whether it was updated</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> CreateOrUpdateNotificationAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges a notification (marks it as read/handled).
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <param name="userId">The user acknowledging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task AcknowledgeNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a notification.
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <param name="userId">The user dismissing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task DismissNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Event Publishing (Convenience Methods)

    /// <summary>
    /// Publishes a SagaStuckEvent to create an appropriate notification.
    /// </summary>
    /// <param name="evt">The event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> PublishSagaStuckEventAsync(
        SagaStuckEvent evt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an ImportCompletedEvent to create an appropriate notification.
    /// </summary>
    /// <param name="evt">The event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> PublishImportCompletedEventAsync(
        ImportCompletedEvent evt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an ImportFailedEvent to create an appropriate notification.
    /// </summary>
    /// <param name="evt">The event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> PublishImportFailedEventAsync(
        ImportFailedEvent evt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes an EscalationCreatedEvent to create an appropriate notification.
    /// </summary>
    /// <param name="evt">The event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> PublishEscalationCreatedEventAsync(
        EscalationCreatedEvent evt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a FileProcessingErrorEvent to create an appropriate notification.
    /// </summary>
    /// <param name="evt">The event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with notification ID</returns>
    /// <exception cref="NotificationServiceException">Thrown when the request fails</exception>
    Task<NotificationResponse> PublishFileProcessingErrorEventAsync(
        FileProcessingErrorEvent evt,
        CancellationToken cancellationToken = default);

    #endregion

    #region Health Check

    /// <summary>
    /// Checks if the NotificationService is healthy and reachable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if healthy, false otherwise</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    #endregion
}
