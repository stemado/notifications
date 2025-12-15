using Quartz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NotificationService.Api.EventHandlers;
using NotificationService.Api.Events;
using NotificationService.Api.Jobs;
using NotificationService.Client.Events;
using Core.ImportHistoryScheduler.Extensions;

// Aliases to avoid ambiguity between Api and Client event types
using ApiSagaStuckEvent = NotificationService.Api.Events.SagaStuckEvent;
using ApiSLABreachEvent = NotificationService.Api.Events.SLABreachEvent;
using ApiPlanSourceOperationFailedEvent = NotificationService.Api.Events.PlanSourceOperationFailedEvent;
using ApiAggregateGenerationStalledEvent = NotificationService.Api.Events.AggregateGenerationStalledEvent;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Services.Channels;
using NotificationService.Infrastructure.Services.Email;
using NotificationService.Infrastructure.Services.Teams;
using NotificationService.Infrastructure.Services.Sms;
using NotificationService.Infrastructure.Services.Templates;
using NotificationService.Routing.Extensions;

namespace NotificationService.Api.Extensions;

/// <summary>
/// Extension methods for configuring notification services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all notification-related services to the DI container
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        // Database
        // Prefer the configured connection string in appsettings.json, but fall back to legacy environment variable if present
        var connectionString = Environment.GetEnvironmentVariable("NOTIFICATIONS_CONNECTION_STRING", EnvironmentVariableTarget.Machine)
            ?? throw new InvalidOperationException("Notification database connection string is not configured. Set 'NOTIFICATIONS_CONNECTION_STRING' in configuration or the IMPORT_PULSE_CONNECTION_STRING environment variable.");

        // In local development, most times Postgres is configured without SSL. Ensure we don't force SSL in dev.
        if (env.IsDevelopment())
        {
            // If the connection string does not specify Ssl Mode (or SslMode), append Ssl Mode=Disable
            // Use a case-insensitive check for the key in the connection string
            if (!connectionString.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("SslMode=", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("SSLMODE=", StringComparison.OrdinalIgnoreCase))
            {
                // Ensure trailing semicolon for append
                if (!connectionString.EndsWith(";"))
                    connectionString += ";";
                connectionString += "Ssl Mode=Disable";
            }
        }

        // Add connection pool health settings to prevent stale connections
        connectionString = AppendConnectionPoolSettings(connectionString);

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            }));

        // Repositories (Phase 1)
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Repositories (Phase 2)
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        // Repositories (Phase 0 - Email Templates)
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();

        // Core services (Phase 1)
        services.AddScoped<INotificationService, Infrastructure.Services.NotificationService>();

        // Services (Phase 2)
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUserService, UserService>();

        // Delivery tracking and channel health services
        services.AddScoped<Infrastructure.Services.Delivery.IDeliveryTrackingService, Infrastructure.Services.Delivery.DeliveryTrackingService>();
        services.AddScoped<IChannelHealthService, ChannelHealthService>();
        services.AddScoped<IChannelConfigurationService, ChannelConfigurationService>();

        // Email services (Phase 2) - Factory pattern for provider selection
        // Provider is configured via "Email:Provider" setting: "Smtp" or "MicrosoftGraph"
        services.Configure<EmailProviderOptions>(configuration.GetSection(EmailProviderOptions.SectionName));
        services.AddSingleton<IEmailServiceFactory, EmailServiceFactory>();
        services.AddScoped<IEmailService, FactoryBasedEmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        // Template rendering services (Phase 0)
        services.AddScoped<ITemplateRenderingService, TemplateRenderingService>();

        // Teams services (Phase 3)
        services.AddHttpClient("Teams");
        services.AddScoped<ITeamsService, TeamsMessageService>();
        services.AddScoped<ITeamsCardService, TeamsCardService>();

        // SMS services (Phase 3)
        services.AddHttpClient("Twilio");
        services.AddScoped<ISmsService, TwilioSmsService>();

        // Event handlers (Phase 1)
        services.AddScoped<IEventHandler<ApiSagaStuckEvent>, SagaStuckNotificationHandler>();

        // Event handlers (Phase 4)
        services.AddScoped<IEventHandler<ImportCompletedEvent>, ImportCompletedNotificationHandler>();
        services.AddScoped<IEventHandler<ImportFailedEvent>, ImportFailedNotificationHandler>();
        services.AddScoped<IEventHandler<EscalationCreatedEvent>, EscalationCreatedNotificationHandler>();
        services.AddScoped<IEventHandler<FileProcessingErrorEvent>, FileProcessingErrorNotificationHandler>();
        services.AddScoped<IEventHandler<FilePickedUpEvent>, FilePickedUpNotificationHandler>();

        // Event handlers (Phase 6 - Supervisor Pattern Integration)
        services.AddScoped<IEventHandler<ApiSLABreachEvent>, SLABreachNotificationHandler>();
        services.AddScoped<IEventHandler<ApiPlanSourceOperationFailedEvent>, PlanSourceOperationFailedNotificationHandler>();
        services.AddScoped<IEventHandler<ApiAggregateGenerationStalledEvent>, AggregateGenerationStalledNotificationHandler>();

        // Event handlers (Phase 7 - Import History Scheduler Integration)
        // This handler triggers ScheduleCheckAsync when templates are queued
        services.AddScoped<IEventHandler<TemplatesQueuedEvent>, TemplatesQueuedNotificationHandler>();

        // Import History Scheduler - required for TemplatesQueuedNotificationHandler
        // Uses MySQL database to persist scheduled checks
        services.AddImportHistoryScheduler();

        // Background jobs (Phase 1)
        services.AddScoped<NotificationRepeatJob>();
        services.AddScoped<NotificationCleanupJob>();
        services.AddScoped<NotificationBackupPollingJob>();

        // SignalR (Phase 1)
        services.AddSignalR();

        // Quartz.NET for background jobs (Phase 1)
        services.AddQuartz(q =>
        {
            // Notification repeat job - runs every 5 minutes
            var repeatJobKey = new JobKey("notification-repeat");
            q.AddJob<NotificationRepeatJob>(opts => opts.WithIdentity(repeatJobKey));
            q.AddTrigger(opts => opts
                .ForJob(repeatJobKey)
                .WithIdentity("notification-repeat-trigger")
                .WithCronSchedule("0 */5 * * * ?") // Every 5 minutes
                .WithDescription("Repeats notifications based on RepeatInterval"));

            // Notification cleanup job - runs daily at 2 AM
            var cleanupJobKey = new JobKey("notification-cleanup");
            q.AddJob<NotificationCleanupJob>(opts => opts.WithIdentity(cleanupJobKey));
            q.AddTrigger(opts => opts
                .ForJob(cleanupJobKey)
                .WithIdentity("notification-cleanup-trigger")
                .WithCronSchedule("0 0 2 * * ?") // Daily at 2 AM
                .WithDescription("Cleans up old and acknowledged notifications"));

            // Notification backup polling job - runs every 15 minutes
            var backupJobKey = new JobKey("notification-backup-polling");
            q.AddJob<NotificationBackupPollingJob>(opts => opts.WithIdentity(backupJobKey));
            q.AddTrigger(opts => opts
                .ForJob(backupJobKey)
                .WithIdentity("notification-backup-polling-trigger")
                .WithCronSchedule("0 */15 * * * ?") // Every 15 minutes
                .WithDescription("Backup polling for stuck sagas"));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Multi-channel dispatcher (Phase 2 - ACTIVE)
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Notification channels (Phase 2+3)
        services.AddScoped<INotificationChannel, SignalRChannel>();
        services.AddScoped<INotificationChannel, EmailChannel>();
        services.AddScoped<INotificationChannel, TeamsChannel>();
        services.AddScoped<INotificationChannel, SmsChannel>();

        // Routing bounded context (outbound notifications to external recipients)
        services.AddRoutingServices(configuration, env);

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
