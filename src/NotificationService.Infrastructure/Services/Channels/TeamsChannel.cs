using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services.Teams;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Microsoft Teams notification channel (Phase 3)
/// </summary>
public class TeamsChannel : INotificationChannel
{
    private readonly ITeamsService _teamsService;
    private readonly ITeamsCardService _cardService;
    private readonly IUserService _userService;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly ILogger<TeamsChannel> _logger;
    private readonly IConfiguration _configuration;

    public TeamsChannel(
        ITeamsService teamsService,
        ITeamsCardService cardService,
        IUserService userService,
        INotificationDeliveryRepository deliveryRepository,
        ILogger<TeamsChannel> logger,
        IConfiguration configuration)
    {
        _teamsService = teamsService;
        _cardService = cardService;
        _userService = userService;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public string ChannelName => "Teams";

    public async Task DeliverAsync(Notification notification, Guid userId)
    {
        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            Channel = NotificationChannel.Teams,
            AttemptCount = 1
        };

        try
        {
            // Get Teams webhook URL for user or use default
            // TODO: Implement per-user webhook lookup when user profiles support it
            var webhookUrl = _configuration["Teams:WebhookUrl"];

            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("No Teams webhook URL configured");
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = "No Teams webhook URL configured";
                await _deliveryRepository.CreateAsync(delivery);
                return;
            }

            // Create Adaptive Card
            var card = _cardService.CreateAdaptiveCard(notification);

            // Send to Teams
            var success = await _teamsService.SendMessageAsync(webhookUrl, card);

            if (success)
            {
                delivery.DeliveredAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Teams notification {NotificationId} delivered to user {UserId}",
                    notification.Id, userId);
            }
            else
            {
                delivery.FailedAt = DateTime.UtcNow;
                delivery.ErrorMessage = "Failed to send message to Teams";
                _logger.LogWarning(
                    "Failed to deliver Teams notification {NotificationId} to user {UserId}",
                    notification.Id, userId);
            }
        }
        catch (Exception ex)
        {
            delivery.FailedAt = DateTime.UtcNow;
            delivery.ErrorMessage = ex.Message;
            _logger.LogError(ex,
                "Error delivering Teams notification {NotificationId} to user {UserId}",
                notification.Id, userId);
        }
        finally
        {
            await _deliveryRepository.CreateAsync(delivery);
        }
    }
}
