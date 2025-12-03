using System.Security.Claims;

namespace NotificationService.Api.Middleware;

/// <summary>
/// Development-only middleware that injects a default system user when authentication is disabled.
/// This allows the NotificationService API to work without Keycloak during development.
/// </summary>
public class DevelopmentUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DevelopmentUserMiddleware> _logger;

    /// <summary>
    /// Default system user ID for development and testing.
    /// This user should exist in Keycloak or be recognized as a valid system user.
    /// </summary>
    public static readonly Guid SYSTEM_USER_ID = new Guid("00000000-0000-0000-0000-000000000001");

    public DevelopmentUserMiddleware(RequestDelegate next, ILogger<DevelopmentUserMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only inject user if not already authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            // Check for X-User-Id header (from MCP tool or testing tools)
            var userId = SYSTEM_USER_ID;
            if (context.Request.Headers.TryGetValue("X-User-Id", out var headerUserId))
            {
                if (Guid.TryParse(headerUserId, out var parsedUserId))
                {
                    userId = parsedUserId;
                    _logger.LogDebug("Using user ID from X-User-Id header: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("Invalid X-User-Id header value: {HeaderValue}. Using system user.", headerUserId);
                }
            }
            else
            {
                _logger.LogDebug("No X-User-Id header provided. Using system user: {UserId}", userId);
            }

            // Inject claims to simulate authenticated user
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userId == SYSTEM_USER_ID ? "System User" : $"Dev User {userId}"),
                new Claim("auth_mode", "development")
            };

            var identity = new ClaimsIdentity(claims, "DevMode");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
