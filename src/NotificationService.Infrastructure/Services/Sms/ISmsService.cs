namespace NotificationService.Infrastructure.Services.Sms;

/// <summary>
/// Service for sending SMS messages
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message
    /// </summary>
    Task<bool> SendSmsAsync(string toPhoneNumber, string message);
}
