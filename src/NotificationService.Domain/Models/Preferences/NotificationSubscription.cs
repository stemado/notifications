using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Models.Preferences;

/// <summary>
/// User subscription to specific clients/sagas (Phase 2)
/// </summary>
public class NotificationSubscription
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Client ID (NULL = all clients)
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Saga ID (NULL = all sagas for the client)
    /// </summary>
    public Guid? SagaId { get; set; }

    /// <summary>
    /// Minimum severity level for this subscription
    /// </summary>
    public NotificationSeverity MinSeverity { get; set; }
}
