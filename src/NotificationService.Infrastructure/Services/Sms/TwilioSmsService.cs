using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace NotificationService.Infrastructure.Services.Sms;

/// <summary>
/// SMS service implementation using Twilio
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public TwilioSmsService(
        ILogger<TwilioSmsService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("Twilio");
    }

    public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
    {
        try
        {
            var accountSid = _configuration["Sms:Twilio:AccountSid"];
            var authToken = _configuration["Sms:Twilio:AuthToken"];
            var fromPhoneNumber = _configuration["Sms:Twilio:FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhoneNumber))
            {
                _logger.LogWarning("Twilio configuration is incomplete. SMS not sent.");
                return false;
            }

            // Build Twilio API URL
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

            // Build form data
            var formData = new Dictionary<string, string>
            {
                { "To", toPhoneNumber },
                { "From", fromPhoneNumber },
                { "Body", message }
            };

            var content = new FormUrlEncodedContent(formData);

            // Add basic authentication
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            // Send request
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}", toPhoneNumber);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send SMS. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", toPhoneNumber);
            return false;
        }
    }
}
