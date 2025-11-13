using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Basic user service implementation (Phase 2)
/// TODO: Replace with integration to your actual user management system
/// </summary>
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    // TODO: Inject your user repository or external user service
    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetUserEmailAsync(Guid userId)
    {
        // TODO: Implement actual user lookup
        // Example: return await _userRepository.GetEmailByIdAsync(userId);

        _logger.LogWarning("UserService.GetUserEmailAsync not fully implemented. Using placeholder for user {UserId}", userId);

        // Placeholder implementation
        await Task.CompletedTask;
        return $"user-{userId}@example.com";
    }

    public async Task<string?> GetUserNameAsync(Guid userId)
    {
        // TODO: Implement actual user lookup
        // Example: return await _userRepository.GetNameByIdAsync(userId);

        _logger.LogWarning("UserService.GetUserNameAsync not fully implemented. Using placeholder for user {UserId}", userId);

        // Placeholder implementation
        await Task.CompletedTask;
        return $"User {userId.ToString().Substring(0, 8)}";
    }
}
