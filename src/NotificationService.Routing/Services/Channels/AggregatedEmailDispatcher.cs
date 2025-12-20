using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.Services.Email;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;

namespace NotificationService.Routing.Services.Channels;

/// <summary>
/// Dispatches multiple outbound deliveries as a single email with proper TO/CC/BCC recipients.
/// This aggregates deliveries by their Role property to construct a properly addressed email.
/// </summary>
public class AggregatedEmailDispatcher : IAggregatedEmailDispatcher
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AggregatedEmailDispatcher> _logger;

    public AggregatedEmailDispatcher(
        IEmailService emailService,
        ILogger<AggregatedEmailDispatcher> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AggregatedDispatchResult> DispatchAggregatedAsync(
        List<OutboundDelivery> deliveries,
        OutboundEvent evt,
        CancellationToken cancellationToken = default)
    {
        if (deliveries.Count == 0)
        {
            return AggregatedDispatchResult.Failed("No deliveries to process", isRetryable: false);
        }

        // Validate all deliveries belong to the same event
        var eventIds = deliveries.Select(d => d.OutboundEventId).Distinct().ToList();
        if (eventIds.Count > 1)
        {
            return AggregatedDispatchResult.Failed(
                $"Cannot aggregate deliveries from multiple events: {string.Join(", ", eventIds)}",
                isRetryable: false);
        }

        // Group deliveries by role and extract email addresses
        var toDeliveries = deliveries
            .Where(d => d.Role == DeliveryRole.To && !string.IsNullOrEmpty(d.Contact?.Email))
            .ToList();

        var ccDeliveries = deliveries
            .Where(d => d.Role == DeliveryRole.Cc && !string.IsNullOrEmpty(d.Contact?.Email))
            .ToList();

        var bccDeliveries = deliveries
            .Where(d => d.Role == DeliveryRole.Bcc && !string.IsNullOrEmpty(d.Contact?.Email))
            .ToList();

        var toEmails = toDeliveries.Select(d => d.Contact!.Email!).Distinct().ToList();
        var ccEmails = ccDeliveries.Select(d => d.Contact!.Email!).Distinct().ToList();
        var bccEmails = bccDeliveries.Select(d => d.Contact!.Email!).Distinct().ToList();

        // Track deliveries with missing emails
        var deliveryResults = new Dictionary<Guid, DeliveryResult>();
        var missingEmailDeliveries = deliveries
            .Where(d => string.IsNullOrEmpty(d.Contact?.Email))
            .ToList();

        foreach (var delivery in missingEmailDeliveries)
        {
            deliveryResults[delivery.Id] = DeliveryResult.Failed(
                delivery.Contact?.Email ?? "(no contact)",
                $"Contact {delivery.ContactId} has no email address");
        }

        // Must have at least one TO recipient
        if (toEmails.Count == 0)
        {
            _logger.LogWarning(
                "No TO recipients found for event {EventId}. CC: {CcCount}, BCC: {BccCount}",
                evt.Id, ccEmails.Count, bccEmails.Count);

            // Mark all deliveries as failed
            foreach (var delivery in deliveries.Where(d => !deliveryResults.ContainsKey(d.Id)))
            {
                deliveryResults[delivery.Id] = DeliveryResult.Failed(
                    delivery.Contact?.Email ?? "(unknown)",
                    "Email requires at least one TO recipient");
            }

            return AggregatedDispatchResult.Failed(
                "Email requires at least one TO recipient - CC/BCC only emails are not valid",
                isRetryable: false,
                deliveryResults);
        }

        var subject = evt.Subject ?? "Notification";
        var body = evt.Body ?? "";

        _logger.LogDebug(
            "Dispatching aggregated email for event {EventId}. TO: {ToCount}, CC: {CcCount}, BCC: {BccCount}",
            evt.Id, toEmails.Count, ccEmails.Count, bccEmails.Count);

        try
        {
            var result = await _emailService.SendEmailAsync(
                toEmails,
                ccEmails.Count > 0 ? ccEmails : null,
                bccEmails.Count > 0 ? bccEmails : null,
                subject,
                body,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Aggregated email sent successfully for event {EventId}. MessageId: {MessageId}, " +
                    "TO: [{To}], CC: [{Cc}], BCC: [{Bcc}]",
                    evt.Id,
                    result.MessageId,
                    string.Join(", ", toEmails),
                    string.Join(", ", ccEmails),
                    string.Join(", ", bccEmails));

                // Mark all deliveries with valid emails as succeeded
                foreach (var delivery in toDeliveries)
                {
                    deliveryResults[delivery.Id] = DeliveryResult.Succeeded(delivery.Contact!.Email!);
                }
                foreach (var delivery in ccDeliveries)
                {
                    deliveryResults[delivery.Id] = DeliveryResult.Succeeded(delivery.Contact!.Email!);
                }
                foreach (var delivery in bccDeliveries)
                {
                    deliveryResults[delivery.Id] = DeliveryResult.Succeeded(delivery.Contact!.Email!);
                }

                return AggregatedDispatchResult.Succeeded(result.MessageId, deliveryResults);
            }

            // Email send failed
            _logger.LogError(
                "Aggregated email send failed for event {EventId}. Error: {Error}",
                evt.Id, result.ErrorMessage);

            foreach (var delivery in deliveries.Where(d => !deliveryResults.ContainsKey(d.Id)))
            {
                deliveryResults[delivery.Id] = DeliveryResult.Failed(
                    delivery.Contact?.Email ?? "(unknown)",
                    result.ErrorMessage ?? "Email send failed");
            }

            // Check if configuration error (not retryable)
            var isRetryable = result.ErrorMessage?.StartsWith("Configuration error") != true;

            return AggregatedDispatchResult.Failed(
                result.ErrorMessage ?? "Email send failed",
                isRetryable,
                deliveryResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception sending aggregated email for event {EventId}",
                evt.Id);

            foreach (var delivery in deliveries.Where(d => !deliveryResults.ContainsKey(d.Id)))
            {
                deliveryResults[delivery.Id] = DeliveryResult.Failed(
                    delivery.Contact?.Email ?? "(unknown)",
                    ex.Message);
            }

            return AggregatedDispatchResult.Failed(ex.Message, deliveryResults: deliveryResults);
        }
    }
}
