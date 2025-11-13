using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using NotificationService.Api.EventHandlers;
using NotificationService.Api.Events;
using NotificationService.Api.Jobs;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Services.Channels;

namespace NotificationService.Api.Extensions;

/// <summary>
/// Extension methods for configuring notification services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all notification-related services to the DI container
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("NotificationDb");
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories (Phase 1)
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Core services (Phase 1)
        services.AddScoped<INotificationService, Infrastructure.Services.NotificationService>();

        // Event handlers (Phase 1)
        services.AddScoped<IEventHandler<SagaStuckEvent>, SagaStuckNotificationHandler>();

        // Background jobs (Phase 1)
        services.AddScoped<NotificationRepeatJob>();
        services.AddScoped<NotificationCleanupJob>();
        services.AddScoped<NotificationBackupPollingJob>();

        // SignalR (Phase 1)
        services.AddSignalR();

        // Hangfire for background jobs (Phase 1)
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        // Multi-channel (Phase 2 - register but don't use yet)
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationChannel, SignalRChannel>();
        // Phase 2: Uncomment when implementing
        // services.AddScoped<INotificationChannel, EmailChannel>();
        // Phase 3: Uncomment when implementing
        // services.AddScoped<INotificationChannel, SlackChannel>();

        return services;
    }

    /// <summary>
    /// Configures Hangfire recurring jobs for notifications
    /// </summary>
    public static IApplicationBuilder UseNotificationJobs(this IApplicationBuilder app)
    {
        // Notification repeat job - runs every 5 minutes
        RecurringJob.AddOrUpdate<NotificationRepeatJob>(
            "notification-repeat",
            job => job.Execute(),
            "*/5 * * * *" // Every 5 minutes
        );

        // Notification cleanup job - runs daily at 2 AM
        RecurringJob.AddOrUpdate<NotificationCleanupJob>(
            "notification-cleanup",
            job => job.Execute(),
            "0 2 * * *" // Daily at 2 AM
        );

        // Notification backup polling job - runs every 15 minutes
        RecurringJob.AddOrUpdate<NotificationBackupPollingJob>(
            "notification-backup-polling",
            job => job.Execute(),
            "*/15 * * * *" // Every 15 minutes
        );

        return app;
    }
}
