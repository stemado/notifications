using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Routing.Data;
using NotificationService.Routing.Repositories;
using NotificationService.Routing.Services;

namespace NotificationService.Routing.Extensions;

/// <summary>
/// Extension methods for configuring routing services
/// </summary>
public static class RoutingServiceCollectionExtensions
{
    /// <summary>
    /// Adds all routing-related services to the DI container
    /// </summary>
    public static IServiceCollection AddRoutingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        // Database - use the same connection string as the main notification database
        var connectionString = Environment.GetEnvironmentVariable("NOTIFICATIONS_CONNECTION_STRING", EnvironmentVariableTarget.Machine)
            ?? throw new InvalidOperationException("NOTIFICATIONS_CONNECTION_STRING environment variable is not set.");

        // In local development, most times Postgres is configured without SSL
        if (env.IsDevelopment())
        {
            if (!connectionString.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("SslMode=", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("SSLMODE=", StringComparison.OrdinalIgnoreCase))
            {
                if (!connectionString.EndsWith(";"))
                    connectionString += ";";
                connectionString += "Ssl Mode=Disable";
            }
        }

        // Add connection pool health settings to prevent stale connections
        connectionString = AppendConnectionPoolSettings(connectionString);

        services.AddDbContext<RoutingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            }));

        // Repositories
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IRecipientGroupRepository, RecipientGroupRepository>();
        services.AddScoped<IRoutingPolicyRepository, RoutingPolicyRepository>();
        services.AddScoped<IOutboundEventRepository, OutboundEventRepository>();
        services.AddScoped<IOutboundDeliveryRepository, OutboundDeliveryRepository>();

        // Services
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IRecipientGroupService, RecipientGroupService>();
        services.AddScoped<IRoutingPolicyService, RoutingPolicyService>();
        services.AddScoped<IOutboundRouter, OutboundRouter>();

        return services;
    }

    /// <summary>
    /// Appends connection pool health settings to prevent stale connections.
    /// These settings help detect and remove dead connections from the pool.
    /// </summary>
    private static string AppendConnectionPoolSettings(string connectionString)
    {
        var sb = new System.Text.StringBuilder(connectionString);

        // Ensure trailing semicolon
        if (!connectionString.EndsWith(";"))
            sb.Append(';');

        // Keepalive: Send TCP keepalive packets every 30 seconds to detect dead connections
        if (!connectionString.Contains("Keepalive", StringComparison.OrdinalIgnoreCase))
            sb.Append("Keepalive=30;");

        // Connection Idle Lifetime: Close connections that have been idle for more than 60 seconds
        if (!connectionString.Contains("Connection Idle Lifetime", StringComparison.OrdinalIgnoreCase))
            sb.Append("Connection Idle Lifetime=60;");

        // Connection Pruning Interval: Check for dead connections every 10 seconds
        if (!connectionString.Contains("Connection Pruning Interval", StringComparison.OrdinalIgnoreCase))
            sb.Append("Connection Pruning Interval=10;");

        // Timeout: Connection timeout of 30 seconds
        if (!connectionString.Contains("Timeout=", StringComparison.OrdinalIgnoreCase))
            sb.Append("Timeout=30;");

        // Command Timeout: Query timeout of 30 seconds
        if (!connectionString.Contains("Command Timeout", StringComparison.OrdinalIgnoreCase))
            sb.Append("Command Timeout=30;");

        // Maximum Pool Size: Limit pool size to prevent connection exhaustion (25 per DbContext)
        if (!connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase))
            sb.Append("Maximum Pool Size=25;");

        return sb.ToString();
    }
}
