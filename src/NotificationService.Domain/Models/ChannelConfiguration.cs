using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Models;

/// <summary>
/// Entity for storing notification channel configuration
/// </summary>
public class ChannelConfiguration
{
    public Guid Id { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool Enabled { get; set; }
    public bool Configured { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public DateTime? LastTestedAt { get; set; }
    public string? TestStatus { get; set; }
    public string? TestError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
