using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Services.Teams;

/// <summary>
/// Service implementation for sending messages to Microsoft Teams
/// </summary>
public class TeamsMessageService : ITeamsService
{
    private readonly ILogger<TeamsMessageService> _logger;
    private readonly HttpClient _httpClient;

    public TeamsMessageService(ILogger<TeamsMessageService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Teams");
    }

    public async Task<bool> SendMessageAsync(string webhookUrl, object message)
    {
        try
        {
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Teams webhook URL is empty");
                return false;
            }

            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent to Teams successfully");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send message to Teams. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Teams");
            return false;
        }
    }
}
