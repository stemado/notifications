using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Models;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Services.Channels;

/// <summary>
/// Service for managing notification channel configurations
/// </summary>
public class ChannelConfigurationService : IChannelConfigurationService
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<ChannelConfigurationService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ChannelConfigurationService(
        NotificationDbContext dbContext,
        ILogger<ChannelConfigurationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ChannelConfigurationResponse> GetConfigurationAsync(NotificationChannel channel)
    {
        var config = await _dbContext.ChannelConfigurations
            .FirstOrDefaultAsync(c => c.Channel == channel);

        if (config == null)
        {
            // Return default configuration
            return new ChannelConfigurationResponse
            {
                Channel = channel.ToString(),
                Enabled = channel == NotificationChannel.SignalR,
                Configured = channel == NotificationChannel.SignalR,
                Config = GetDefaultConfig(channel)
            };
        }

        return MapToResponse(config);
    }

    public async Task<IEnumerable<ChannelConfigurationResponse>> GetAllConfigurationsAsync()
    {
        var configs = await _dbContext.ChannelConfigurations.ToListAsync();

        // Ensure all channels are represented
        var allChannels = Enum.GetValues<NotificationChannel>();
        var result = new List<ChannelConfigurationResponse>();

        foreach (var channel in allChannels)
        {
            var config = configs.FirstOrDefault(c => c.Channel == channel);
            if (config != null)
            {
                result.Add(MapToResponse(config));
            }
            else
            {
                result.Add(new ChannelConfigurationResponse
                {
                    Channel = channel.ToString(),
                    Enabled = channel == NotificationChannel.SignalR,
                    Configured = channel == NotificationChannel.SignalR,
                    Config = GetDefaultConfig(channel)
                });
            }
        }

        return result;
    }

    public async Task<ChannelConfigurationResponse> UpdateConfigurationAsync(NotificationChannel channel, UpdateChannelConfigurationRequest request)
    {
        _logger.LogInformation("Updating configuration for channel {Channel}", channel);

        var config = await _dbContext.ChannelConfigurations
            .FirstOrDefaultAsync(c => c.Channel == channel);

        if (config == null)
        {
            // Create new configuration
            config = new ChannelConfiguration
            {
                Id = Guid.NewGuid(),
                Channel = channel,
                Enabled = request.Enabled,
                ConfigurationJson = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ChannelConfigurations.Add(config);
        }
        else
        {
            config.Enabled = request.Enabled;
            config.UpdatedAt = DateTime.UtcNow;
        }

        // Build configuration JSON based on channel type
        var configObj = BuildConfigObject(channel, request, config.ConfigurationJson);
        config.ConfigurationJson = JsonSerializer.Serialize(configObj, JsonOptions);
        config.Configured = IsConfigured(channel, configObj);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Configuration updated for channel {Channel}, Enabled: {Enabled}, Configured: {Configured}",
            channel, config.Enabled, config.Configured);

        return MapToResponse(config);
    }

    public async Task EnableChannelAsync(NotificationChannel channel)
    {
        var config = await GetOrCreateConfigAsync(channel);
        config.Enabled = true;
        config.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Channel {Channel} enabled", channel);
    }

    public async Task DisableChannelAsync(NotificationChannel channel)
    {
        var config = await GetOrCreateConfigAsync(channel);
        config.Enabled = false;
        config.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Channel {Channel} disabled", channel);
    }

    public async Task<TestChannelResult> TestChannelAsync(NotificationChannel channel)
    {
        _logger.LogInformation("Testing channel {Channel}", channel);

        var config = await _dbContext.ChannelConfigurations
            .FirstOrDefaultAsync(c => c.Channel == channel);

        if (config == null || !config.Configured)
        {
            return new TestChannelResult
            {
                Success = false,
                Message = "Channel is not configured"
            };
        }

        // TODO: Implement actual channel testing based on type
        // For now, just simulate a test
        var result = channel switch
        {
            NotificationChannel.SignalR => new TestChannelResult { Success = true, Message = "SignalR hub is running" },
            NotificationChannel.Email => TestEmailChannel(config),
            NotificationChannel.SMS => TestSmsChannel(config),
            NotificationChannel.Teams => TestTeamsChannel(config),
            _ => new TestChannelResult { Success = false, Message = "Unknown channel" }
        };

        // Update test status
        config.LastTestedAt = DateTime.UtcNow;
        config.TestStatus = result.Success ? "success" : "failed";
        config.TestError = result.Success ? null : result.Message;
        await _dbContext.SaveChangesAsync();

        return result;
    }

    private async Task<ChannelConfiguration> GetOrCreateConfigAsync(NotificationChannel channel)
    {
        var config = await _dbContext.ChannelConfigurations
            .FirstOrDefaultAsync(c => c.Channel == channel);

        if (config == null)
        {
            config = new ChannelConfiguration
            {
                Id = Guid.NewGuid(),
                Channel = channel,
                Enabled = false,
                Configured = false,
                ConfigurationJson = JsonSerializer.Serialize(GetDefaultConfig(channel), JsonOptions),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ChannelConfigurations.Add(config);
            await _dbContext.SaveChangesAsync();
        }

        return config;
    }

    private ChannelConfigurationResponse MapToResponse(ChannelConfiguration config)
    {
        object? configObj = null;
        try
        {
            var jsonDoc = JsonDocument.Parse(config.ConfigurationJson);
            configObj = MaskSensitiveData(config.Channel, jsonDoc.RootElement);
        }
        catch
        {
            configObj = new { };
        }

        return new ChannelConfigurationResponse
        {
            Channel = config.Channel.ToString(),
            Enabled = config.Enabled,
            Configured = config.Configured,
            Config = configObj,
            LastTestedAt = config.LastTestedAt,
            TestStatus = config.TestStatus,
            TestError = config.TestError
        };
    }

    private static object GetDefaultConfig(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.SignalR => new { hubUrl = "/hubs/notifications", autoReconnect = true },
            NotificationChannel.Email => new { provider = "graph", smtpPort = 587, enableSsl = true },
            NotificationChannel.SMS => new { provider = "twilio" },
            NotificationChannel.Teams => new { },
            _ => new { }
        };
    }

    private static object BuildConfigObject(NotificationChannel channel, UpdateChannelConfigurationRequest request, string existingJson)
    {
        // Parse existing config
        Dictionary<string, object?> config;
        try
        {
            config = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingJson) ?? new();
        }
        catch
        {
            config = new();
        }

        // Update based on channel type
        switch (channel)
        {
            case NotificationChannel.Email:
                if (request.Provider != null) config["provider"] = request.Provider;
                if (request.SmtpHost != null) config["smtpHost"] = request.SmtpHost;
                if (request.SmtpPort.HasValue) config["smtpPort"] = request.SmtpPort.Value;
                if (request.SmtpUsername != null) config["smtpUsername"] = request.SmtpUsername;
                if (request.SmtpPassword != null) config["smtpPassword"] = request.SmtpPassword;
                if (request.FromAddress != null) config["fromAddress"] = request.FromAddress;
                if (request.ReplyToAddress != null) config["replyToAddress"] = request.ReplyToAddress;
                if (request.EnableSsl.HasValue) config["enableSsl"] = request.EnableSsl.Value;
                break;

            case NotificationChannel.SMS:
                if (request.AccountSid != null) config["accountSid"] = request.AccountSid;
                if (request.AuthToken != null) config["authToken"] = request.AuthToken;
                if (request.FromPhoneNumber != null) config["fromPhoneNumber"] = request.FromPhoneNumber;
                break;

            case NotificationChannel.Teams:
                if (request.WebhookUrl != null) config["webhookUrl"] = request.WebhookUrl;
                if (request.ChannelName != null) config["channelName"] = request.ChannelName;
                break;
        }

        return config;
    }

    private static bool IsConfigured(NotificationChannel channel, object configObj)
    {
        var json = JsonSerializer.Serialize(configObj);
        var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        return channel switch
        {
            NotificationChannel.SignalR => true,
            NotificationChannel.Email => config != null &&
                (config.ContainsKey("fromAddress") && !string.IsNullOrEmpty(config["fromAddress"].GetString())),
            NotificationChannel.SMS => config != null &&
                config.ContainsKey("accountSid") && !string.IsNullOrEmpty(config["accountSid"].GetString()) &&
                config.ContainsKey("authToken") && !string.IsNullOrEmpty(config["authToken"].GetString()),
            NotificationChannel.Teams => config != null &&
                config.ContainsKey("webhookUrl") && !string.IsNullOrEmpty(config["webhookUrl"].GetString()),
            _ => false
        };
    }

    private static object MaskSensitiveData(NotificationChannel channel, JsonElement element)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var prop in element.EnumerateObject())
        {
            var isSensitive = prop.Name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                              prop.Name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                              prop.Name.Contains("secret", StringComparison.OrdinalIgnoreCase);

            if (isSensitive && prop.Value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(prop.Value.GetString()))
            {
                dict[prop.Name] = "********";
            }
            else
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.GetRawText()
                };
            }
        }

        return dict;
    }

    private TestChannelResult TestEmailChannel(ChannelConfiguration config)
    {
        // Basic validation for email config
        try
        {
            var json = JsonDocument.Parse(config.ConfigurationJson);
            var root = json.RootElement;

            if (root.TryGetProperty("fromAddress", out var fromAddr) && !string.IsNullOrEmpty(fromAddr.GetString()))
            {
                return new TestChannelResult { Success = true, Message = "Email configuration is valid" };
            }
            return new TestChannelResult { Success = false, Message = "From address is not configured" };
        }
        catch (Exception ex)
        {
            return new TestChannelResult { Success = false, Message = ex.Message };
        }
    }

    private TestChannelResult TestSmsChannel(ChannelConfiguration config)
    {
        try
        {
            var json = JsonDocument.Parse(config.ConfigurationJson);
            var root = json.RootElement;

            if (root.TryGetProperty("accountSid", out var sid) && !string.IsNullOrEmpty(sid.GetString()) &&
                root.TryGetProperty("authToken", out var token) && !string.IsNullOrEmpty(token.GetString()))
            {
                return new TestChannelResult { Success = true, Message = "SMS (Twilio) configuration is valid" };
            }
            return new TestChannelResult { Success = false, Message = "Twilio credentials are incomplete" };
        }
        catch (Exception ex)
        {
            return new TestChannelResult { Success = false, Message = ex.Message };
        }
    }

    private TestChannelResult TestTeamsChannel(ChannelConfiguration config)
    {
        try
        {
            var json = JsonDocument.Parse(config.ConfigurationJson);
            var root = json.RootElement;

            if (root.TryGetProperty("webhookUrl", out var url) && !string.IsNullOrEmpty(url.GetString()))
            {
                return new TestChannelResult { Success = true, Message = "Teams webhook is configured" };
            }
            return new TestChannelResult { Success = false, Message = "Webhook URL is not configured" };
        }
        catch (Exception ex)
        {
            return new TestChannelResult { Success = false, Message = ex.Message };
        }
    }
}
