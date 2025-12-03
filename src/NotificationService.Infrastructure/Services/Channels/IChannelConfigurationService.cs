using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Service for managing notification channel configurations
/// </summary>
public interface IChannelConfigurationService
{
    /// <summary>
    /// Get configuration for a specific channel
    /// </summary>
    Task<ChannelConfigurationResponse> GetConfigurationAsync(NotificationChannel channel);

    /// <summary>
    /// Get configurations for all channels
    /// </summary>
    Task<IEnumerable<ChannelConfigurationResponse>> GetAllConfigurationsAsync();

    /// <summary>
    /// Update configuration for a specific channel
    /// </summary>
    Task<ChannelConfigurationResponse> UpdateConfigurationAsync(NotificationChannel channel, UpdateChannelConfigurationRequest request);

    /// <summary>
    /// Enable a channel
    /// </summary>
    Task EnableChannelAsync(NotificationChannel channel);

    /// <summary>
    /// Disable a channel
    /// </summary>
    Task DisableChannelAsync(NotificationChannel channel);

    /// <summary>
    /// Test a channel's connectivity
    /// </summary>
    Task<TestChannelResult> TestChannelAsync(NotificationChannel channel);
}
