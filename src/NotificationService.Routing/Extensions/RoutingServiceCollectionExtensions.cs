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

        services.AddDbContext<RoutingDbContext>(options =>
            options.UseNpgsql(connectionString));

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
}
