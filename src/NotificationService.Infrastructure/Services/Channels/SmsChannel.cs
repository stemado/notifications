using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services.Sms;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// SMS notification channel (Phase 3)
/// </summary>
public class SmsChannel : INotificationChannel
{
    private readonly ISmsService _smsService;
    private readonly IUserService _userService;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly ILogger<SmsChannel> _logger;

    public SmsChannel(
        ISmsService smsService,
        IUserService userService,
        INotificationDeliveryRepository deliveryRepository,
        ILogger<SmsChannel> logger)
    {
        _smsService = smsService;
        _userService = userService;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
    }

    public string ChannelName => "SMS";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            Channel = NotificationChannel.SMS,
            AttemptCount = 1
        };

        try
        {
            // Get user phone number
            // TODO: Implement GetUserPhoneNumberAsync in IUserService
            // For now, log a warning
            var userPhone = await GetUserPhoneNumberAsync(userId);

            if (string.IsNullOrEmpty(userPhone))
            {
                _logger.LogWarning("No phone number found for user {UserId}", userId);
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = "No phone number found for user";
                await _deliveryRepository.CreateAsync(delivery);
                return;
            }

            // Format SMS message (max 160 characters for standard SMS)
            var smsMessage = FormatSmsMessage(notification);

            // Send SMS
            var success = await _smsService.SendSmsAsync(userPhone, smsMessage);

            if (success)
            {
                delivery.DeliveredAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "SMS notification {NotificationId} delivered to user {UserId}",
                    notification.Id, userId);
            }
            else
            {
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = "Failed to send SMS";
                _logger.LogWarning(
                    "Failed to deliver SMS notification {NotificationId} to user {UserId}",
                    notification.Id, userId);
            }
        }
        catch (Exception ex)
        {
            delivery.FailedAt = DateTime.UtcNow;
            delivery.ErrorMessage = ex.Message;
            _logger.LogError(ex,
                "Error delivering SMS notification {NotificationId} to user {UserId}",
                notification.Id, userId);
        }
        finally
        {
            await _deliveryRepository.CreateAsync(delivery);
        }
    }

    private string FormatSmsMessage(Notification notification)
    {
        // Format: [SEVERITY] Title: Message
        var severityPrefix = notification.Severity switch
        {
            NotificationSeverity.Critical => "[CRITICAL]",
            NotificationSeverity.Urgent => "[URGENT]",
            NotificationSeverity.Warning => "[WARNING]",
            _ => "[INFO]"
        };

        var message = $"{severityPrefix} {notification.Title}: {notification.Message}";

        // Truncate to 160 characters if needed (standard SMS length)
        if (message.Length > 160)
        {
            message = message.Substring(0, 157) + "...";
        }

        return message;
    }

    private async Task<string?> GetUserPhoneNumberAsync(Guid userId)
    {
        // TODO: Implement this method in IUserService
        // For now, return a placeholder
        await Task.CompletedTask;
        _logger.LogWarning("GetUserPhoneNumberAsync not implemented. Using placeholder.");
        return null; // Return null to indicate no phone number available
    }
}
