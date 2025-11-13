using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services.Email;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Email notification channel (Phase 2)
/// </summary>
public class EmailChannel : INotificationChannel
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly IUserService _userService;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly ILogger<EmailChannel> _logger;

    public EmailChannel(
        IEmailService emailService,
        IEmailTemplateService templateService,
        IUserService userService,
        INotificationDeliveryRepository deliveryRepository,
        ILogger<EmailChannel> logger)
    {
        _emailService = emailService;
        _templateService = templateService;
        _userService = userService;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
    }

    public string ChannelName => "Email";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            Channel = NotificationChannel.Email,
            AttemptCount = 1
        };

        try
        {
            // Get user email address
            var userEmail = await _userService.GetUserEmailAsync(userId);
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("No email address found for user {UserId}", userId);
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = "No email address found for user";
                await _deliveryRepository.CreateAsync(delivery);
                return;
            }

            // Generate email content
            var subject = _templateService.GenerateSubject(notification);
            var htmlBody = _templateService.RenderNotificationHtml(notification);
            var plainTextBody = _templateService.RenderNotificationPlainText(notification);

            // Send email
            var success = await _emailService.SendEmailAsync(userEmail, subject, htmlBody, plainTextBody);

            if (success)
            {
                delivery.DeliveredAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Email notification {NotificationId} delivered to {UserEmail}",
                    notification.Id, userEmail);
            }
            else
            {
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = "Failed to send email";
                _logger.LogWarning(
                    "Failed to deliver email notification {NotificationId} to {UserEmail}",
                    notification.Id, userEmail);
            }
        }
        catch (Exception ex)
        {
            delivery.FailedAt = DateTime.UtcNow;
            delivery.ErrorMessage = ex.Message;
            _logger.LogError(ex,
                "Error delivering email notification {NotificationId} to user {UserId}",
                notification.Id, userId);
        }
        finally
        {
            await _deliveryRepository.CreateAsync(delivery);
        }
    }
}
