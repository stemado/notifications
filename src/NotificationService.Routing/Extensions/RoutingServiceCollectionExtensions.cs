using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Routing.Consumers;
using NotificationService.Routing.Data;
using NotificationService.Routing.Messaging;
using NotificationService.Routing.Repositories;
using NotificationService.Routing.Services;
using NotificationService.Routing.Services.Channels;

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
        services.AddScoped<ITestEmailDeliveryRepository, TestEmailDeliveryRepository>();
        services.AddScoped<IClientAttestationRepository, ClientAttestationRepository>();

        // Services
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IRecipientGroupService, RecipientGroupService>();
        services.AddScoped<IRoutingPolicyService, RoutingPolicyService>();
        services.AddScoped<IOutboundRouter, OutboundRouter>();
        services.AddScoped<IRoutingDashboardService, RoutingDashboardService>();
        services.AddScoped<IClientAttestationService, ClientAttestationService>();

        // Channel dispatcher for routing deliveries to Email/SMS/Teams
        services.AddScoped<IChannelDispatcher, ChannelDispatcher>();

        // Message publisher for outbound deliveries
        services.AddScoped<IDeliveryMessagePublisher, DeliveryMessagePublisher>();

        // MassTransit with RabbitMQ and EF Core Outbox
        services.AddMassTransitForRouting(configuration);

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

    /// <summary>
    /// Configures MassTransit with configurable transport (InMemory or RabbitMQ) and EF Core outbox.
    /// Transport is selected via "Messaging:Transport" config setting.
    /// The outbox provides transactional delivery guarantees regardless of transport.
    /// </summary>
    private static IServiceCollection AddMassTransitForRouting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var transport = configuration.GetValue<string>("Messaging:Transport") ?? "InMemory";

        services.AddMassTransit(x =>
        {
            // Register the consumer
            x.AddConsumer<DeliveryRequestedConsumer>();

            // Configure transport based on settings
            if (transport.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                ConfigureRabbitMqTransport(x, configuration);
            }
            else
            {
                ConfigureInMemoryTransport(x);
            }

            // Configure EF Core outbox for transactional publishing
            // This works with both InMemory and RabbitMQ transports
            x.AddEntityFrameworkOutbox<RoutingDbContext>(o =>
            {
                // Use PostgreSQL for the outbox
                o.UsePostgres();

                // Query delay for outbox message delivery
                o.QueryDelay = TimeSpan.FromSeconds(1);

                // Enable the bus outbox for automatic message staging
                o.UseBusOutbox();
            });
        });

        return services;
    }

    /// <summary>
    /// Configures InMemory transport for local development/testing.
    /// Messages are processed in-process without external dependencies.
    /// </summary>
    private static void ConfigureInMemoryTransport(IBusRegistrationConfigurator x)
    {
        x.UsingInMemory((context, cfg) =>
        {
            // Configure the delivery request queue
            cfg.ReceiveEndpoint("notification-delivery-requests", e =>
            {
                // Configure retry policy
                e.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(5),
                    maxInterval: TimeSpan.FromMinutes(5),
                    intervalDelta: TimeSpan.FromSeconds(10)));

                // Configure the consumer
                e.ConfigureConsumer<DeliveryRequestedConsumer>(context);
            });

            // Configure endpoints for all consumers
            cfg.ConfigureEndpoints(context);
        });
    }

    /// <summary>
    /// Configures RabbitMQ transport for production use.
    /// Requires RabbitMQ server to be running.
    /// </summary>
    private static void ConfigureRabbitMqTransport(
        IBusRegistrationConfigurator x,
        IConfiguration configuration)
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMqSection = configuration.GetSection("RabbitMq");
            var host = rabbitMqSection["Host"] ?? "localhost";
            var virtualHost = rabbitMqSection["VirtualHost"] ?? "/";
            var username = rabbitMqSection["Username"] ?? "guest";
            var password = rabbitMqSection["Password"] ?? "guest";
            var port = rabbitMqSection.GetValue<ushort?>("Port") ?? 5672;

            cfg.Host(host, port, virtualHost, h =>
            {
                h.Username(username);
                h.Password(password);
            });

            // Configure the delivery request queue
            cfg.ReceiveEndpoint("notification-delivery-requests", e =>
            {
                // Configure retry policy
                e.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(5),
                    maxInterval: TimeSpan.FromMinutes(5),
                    intervalDelta: TimeSpan.FromSeconds(10)));

                // Configure the consumer
                e.ConfigureConsumer<DeliveryRequestedConsumer>(context);

                // Prefetch for better throughput
                e.PrefetchCount = 16;
            });

            // Configure endpoints for all consumers
            cfg.ConfigureEndpoints(context);
        });
    }
}
