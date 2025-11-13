namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Service for user information (Phase 2)
/// Note: This would typically integrate with your existing user management system
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets user's email address
    /// </summary>
    Task<string?> GetUserEmailAsync(Guid userId);

    /// <summary>
    /// Gets user's display name
    /// </summary>
    Task<string?> GetUserNameAsync(Guid userId);
}
