using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Multi-channel notification dispatcher interface (Phase 2)
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Dispatches a notification through all enabled channels based on user preferences
    /// </summary>
    Task DispatchAsync(Notification notification);
}
