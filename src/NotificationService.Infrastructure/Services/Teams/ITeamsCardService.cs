using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Teams;

/// <summary>
/// Service for formatting notifications as Teams Adaptive Cards
/// </summary>
public interface ITeamsCardService
{
    /// <summary>
    /// Creates an Adaptive Card for a notification
    /// </summary>
    object CreateAdaptiveCard(Notification notification);
}
