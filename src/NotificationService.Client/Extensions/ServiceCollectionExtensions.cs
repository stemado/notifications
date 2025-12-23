using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Client.Configuration;
using NotificationService.Client.Routing;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace NotificationService.Client.Extensions;

/// <summary>
/// Extension methods for registering NotificationService client in DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NotificationService client with Polly resilience policies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing NotificationService section</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNotificationServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new NotificationServiceOptions();
        configuration.GetSection(NotificationServiceOptions.SectionName).Bind(options);

        return services.AddNotificationServiceClient(opts =>
        {
            opts.BaseUrl = options.BaseUrl;
            opts.TimeoutSeconds = options.TimeoutSeconds;
            opts.RetryCount = options.RetryCount;
            opts.RetryBaseDelayMs = options.RetryBaseDelayMs;
            opts.CircuitBreakerThreshold = options.CircuitBreakerThreshold;
            opts.CircuitBreakerDurationSeconds = options.CircuitBreakerDurationSeconds;
            opts.CircuitBreakerSamplingSeconds = options.CircuitBreakerSamplingSeconds;
            opts.ApiKey = options.ApiKey;
        });
    }

    /// <summary>
    /// Adds the NotificationService client with custom configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNotificationServiceClient(
        this IServiceCollection services,
        Action<NotificationServiceOptions> configureOptions)
    {
        var options = new NotificationServiceOptions();
        configureOptions(options);
        options.Validate();

        services.Configure<NotificationServiceOptions>(opts =>
        {
            opts.BaseUrl = options.BaseUrl;
            opts.TimeoutSeconds = options.TimeoutSeconds;
            opts.RetryCount = options.RetryCount;
            opts.RetryBaseDelayMs = options.RetryBaseDelayMs;
            opts.CircuitBreakerThreshold = options.CircuitBreakerThreshold;
            opts.CircuitBreakerDurationSeconds = options.CircuitBreakerDurationSeconds;
            opts.CircuitBreakerSamplingSeconds = options.CircuitBreakerSamplingSeconds;
            opts.ApiKey = options.ApiKey;
        });

        services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(
                (serviceProvider, client) =>
                {
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.DefaultRequestHeaders.Add("User-Agent", "NotificationService.Client/1.0");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    if (!string.IsNullOrEmpty(options.ApiKey))
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
                    }
                })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddResilienceHandler("NotificationService", (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<NotificationServiceClient>>();

                // 1. Timeout strategy (innermost - applies to each attempt)
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                    OnTimeout = args =>
                    {
                        logger.LogWarning("NotificationService request timed out after {Timeout}s",
                            options.TimeoutSeconds);
                        return default;
                    }
                });

                // 2. Retry strategy
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = options.RetryCount,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs),
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response =>
                            response.StatusCode == HttpStatusCode.RequestTimeout ||
                            response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode >= 500),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "NotificationService request retry {Attempt}/{MaxAttempts}. " +
                            "Status: {StatusCode}, Delay: {Delay}ms",
                            args.AttemptNumber,
                            options.RetryCount,
                            args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.GetType().Name,
                            args.RetryDelay.TotalMilliseconds);
                        return default;
                    }
                });

                // 3. Circuit breaker strategy (outermost - protects service from cascading failures)
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5, // Open after 50% failure rate
                    MinimumThroughput = options.CircuitBreakerThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response => (int)response.StatusCode >= 500),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "NotificationService circuit breaker OPENED. " +
                            "Break duration: {BreakDuration}s. Reason: {Reason}",
                            options.CircuitBreakerDurationSeconds,
                            args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("NotificationService circuit breaker CLOSED. Service recovered.");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("NotificationService circuit breaker HALF-OPEN. Testing service...");
                        return default;
                    }
                });
            });

        return services;
    }

    /// <summary>
    /// Adds the RoutingService client for publishing outbound events with Polly resilience policies.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing RoutingService section</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRoutingServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RoutingServiceOptions();
        configuration.GetSection(RoutingServiceOptions.SectionName).Bind(options);

        return services.AddRoutingServiceClient(opts =>
        {
            opts.BaseUrl = options.BaseUrl;
            opts.TimeoutSeconds = options.TimeoutSeconds;
            opts.RetryCount = options.RetryCount;
            opts.RetryBaseDelayMs = options.RetryBaseDelayMs;
            opts.CircuitBreakerThreshold = options.CircuitBreakerThreshold;
            opts.CircuitBreakerDurationSeconds = options.CircuitBreakerDurationSeconds;
            opts.Enabled = options.Enabled;
            opts.ApiKey = options.ApiKey;
        });
    }

    /// <summary>
    /// Adds the RoutingService client with custom configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRoutingServiceClient(
        this IServiceCollection services,
        Action<RoutingServiceOptions> configureOptions)
    {
        var options = new RoutingServiceOptions();
        configureOptions(options);
        options.Validate();

        services.Configure<RoutingServiceOptions>(opts =>
        {
            opts.BaseUrl = options.BaseUrl;
            opts.TimeoutSeconds = options.TimeoutSeconds;
            opts.RetryCount = options.RetryCount;
            opts.RetryBaseDelayMs = options.RetryBaseDelayMs;
            opts.CircuitBreakerThreshold = options.CircuitBreakerThreshold;
            opts.CircuitBreakerDurationSeconds = options.CircuitBreakerDurationSeconds;
            opts.Enabled = options.Enabled;
            opts.ApiKey = options.ApiKey;
        });

        services.AddHttpClient<IRoutingServiceClient, RoutingServiceClient>(
                (serviceProvider, client) =>
                {
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.DefaultRequestHeaders.Add("User-Agent", "NotificationService.Client/1.0");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    if (!string.IsNullOrEmpty(options.ApiKey))
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
                    }
                })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddResilienceHandler("RoutingService", (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<RoutingServiceClient>>();

                // 1. Timeout strategy (innermost - applies to each attempt)
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                    OnTimeout = args =>
                    {
                        logger.LogWarning("RoutingService request timed out after {Timeout}s",
                            options.TimeoutSeconds);
                        return default;
                    }
                });

                // 2. Retry strategy
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = options.RetryCount,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs),
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response =>
                            response.StatusCode == HttpStatusCode.RequestTimeout ||
                            response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode >= 500),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "RoutingService request retry {Attempt}/{MaxAttempts}. " +
                            "Status: {StatusCode}, Delay: {Delay}ms",
                            args.AttemptNumber,
                            options.RetryCount,
                            args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.GetType().Name,
                            args.RetryDelay.TotalMilliseconds);
                        return default;
                    }
                });

                // 3. Circuit breaker strategy (outermost - protects service from cascading failures)
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5, // Open after 50% failure rate
                    MinimumThroughput = options.CircuitBreakerThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response => (int)response.StatusCode >= 500),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "RoutingService circuit breaker OPENED. " +
                            "Break duration: {BreakDuration}s. Reason: {Reason}",
                            options.CircuitBreakerDurationSeconds,
                            args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("RoutingService circuit breaker CLOSED. Service recovered.");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("RoutingService circuit breaker HALF-OPEN. Testing service...");
                        return default;
                    }
                });
            });

        return services;
    }
}
