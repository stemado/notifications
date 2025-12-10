using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Services.Email;
using NotificationService.Infrastructure.Services.Sms;
using NotificationService.Infrastructure.Services.Teams;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services.Channels;

/// <summary>
/// Dispatches outbound deliveries to the appropriate channel.
/// Routes based on the delivery's Channel property to Email, SMS, or Teams.
/// </summary>
public class ChannelDispatcher : IChannelDispatcher
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ITeamsService _teamsService;
    private readonly ILogger<ChannelDispatcher> _logger;

    public ChannelDispatcher(
        IEmailService emailService,
        ISmsService smsService,
        ITeamsService teamsService,
        ILogger<ChannelDispatcher> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _teamsService = teamsService;
        _logger = logger;
    }

    public async Task<ChannelDispatchResult> DispatchAsync(
        OutboundDelivery delivery,
        OutboundEvent evt,
        CancellationToken cancellationToken = default)
    {
        return delivery.Channel switch
        {
            NotificationChannel.Email => await DispatchEmailAsync(delivery, evt, cancellationToken),
            NotificationChannel.SMS => await DispatchSmsAsync(delivery, evt, cancellationToken),
            NotificationChannel.Teams => await DispatchTeamsAsync(delivery, evt, cancellationToken),
            NotificationChannel.SignalR => ChannelDispatchResult.Failed(
                "SignalR channel not supported for outbound routing", isRetryable: false),
            _ => ChannelDispatchResult.Failed(
                $"Unknown channel: {delivery.Channel}", isRetryable: false)
        };
    }

    private async Task<ChannelDispatchResult> DispatchEmailAsync(
        OutboundDelivery delivery,
        OutboundEvent evt,
        CancellationToken cancellationToken)
    {
        var email = delivery.Contact?.Email;
        if (string.IsNullOrEmpty(email))
        {
            return ChannelDispatchResult.Failed(
                $"Contact {delivery.ContactId} has no email address",
                isRetryable: false);
        }

        var subject = evt.Subject ?? "Notification";
        var body = evt.Body ?? "";

        _logger.LogDebug(
            "Dispatching email to {Email} for delivery {DeliveryId}",
            email, delivery.Id);

        try
        {
            var result = await _emailService.SendEmailAsync(
                email,
                subject,
                body,
                plainTextBody: null,
                ct: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Email sent successfully to {Email} for delivery {DeliveryId}",
                    email, delivery.Id);
                return ChannelDispatchResult.Succeeded(result.MessageId);
            }

            // Consider most email failures retryable unless it's a configuration error
            var isRetryable = result.ErrorMessage?.StartsWith("Configuration error") != true;
            return ChannelDispatchResult.Failed(
                result.ErrorMessage ?? "Email send failed",
                isRetryable: isRetryable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception sending email to {Email} for delivery {DeliveryId}",
                email, delivery.Id);
            return ChannelDispatchResult.Failed(ex.Message);
        }
    }

    private async Task<ChannelDispatchResult> DispatchSmsAsync(
        OutboundDelivery delivery,
        OutboundEvent evt,
        CancellationToken cancellationToken)
    {
        var phone = delivery.Contact?.Phone;
        if (string.IsNullOrEmpty(phone))
        {
            return ChannelDispatchResult.Failed(
                $"Contact {delivery.ContactId} has no phone number",
                isRetryable: false);
        }

        var message = evt.Body ?? evt.Subject ?? "Notification";

        _logger.LogDebug(
            "Dispatching SMS to {Phone} for delivery {DeliveryId}",
            phone, delivery.Id);

        try
        {
            var success = await _smsService.SendSmsAsync(phone, message);

            if (success)
            {
                _logger.LogInformation(
                    "SMS sent successfully to {Phone} for delivery {DeliveryId}",
                    phone, delivery.Id);
                return ChannelDispatchResult.Succeeded();
            }

            return ChannelDispatchResult.Failed("SMS send failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception sending SMS to {Phone} for delivery {DeliveryId}",
                phone, delivery.Id);
            return ChannelDispatchResult.Failed(ex.Message);
        }
    }

    private async Task<ChannelDispatchResult> DispatchTeamsAsync(
        OutboundDelivery delivery,
        OutboundEvent evt,
        CancellationToken cancellationToken)
    {
        // Teams delivery requires a webhook URL, which would typically be
        // configured at the group or contact level. For now, we'll use a
        // simple message format.

        // TODO: Get webhook URL from contact metadata or routing policy
        _logger.LogWarning(
            "Teams channel not fully implemented for delivery {DeliveryId}",
            delivery.Id);

        return ChannelDispatchResult.Failed(
            "Teams channel webhook URL not configured",
            isRetryable: false);
    }
}
