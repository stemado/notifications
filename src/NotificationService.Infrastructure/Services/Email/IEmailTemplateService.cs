using NotificationService.Domain.Models;

namespace NotificationService.Infrastructure.Services.Email;

/// <summary>
/// Service for rendering email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders a notification as HTML email
    /// </summary>
    string RenderNotificationHtml(Notification notification);

    /// <summary>
    /// Renders a notification as plain text email
    /// </summary>
    string RenderNotificationPlainText(Notification notification);

    /// <summary>
    /// Generates email subject for a notification
    /// </summary>
    string GenerateSubject(Notification notification);
}
