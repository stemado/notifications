using NotificationService.Client.Routing.Models;

namespace NotificationService.Client.Routing;

/// <summary>
/// Extension methods for publishing common error notifications.
/// These methods simplify publishing error events from services.
/// </summary>
public static class ErrorNotificationExtensions
{
    /// <summary>
    /// Publishes a service error notification.
    /// </summary>
    public static Task<OutboundEventResponse> PublishServiceErrorAsync(
        this IRoutingServiceClient client,
        string serviceName,
        string clientId,
        string errorMessage,
        Exception? exception = null,
        string severity = "Error",
        Guid? sagaId = null,
        CancellationToken cancellationToken = default)
    {
        var topic = serviceName switch
        {
            "ImportProcessor" => "ImportProcessorError",
            "ImportHistoryProcessor" => "ImportHistoryProcessorError",
            "CensusOrchestration" => "OrchestrationServiceError",
            _ => "UnhandledException"
        };

        return client.PublishEventAsync(new OutboundEventRequest
        {
            Service = serviceName,
            Topic = topic,
            ClientId = clientId,
            Severity = severity,
            Subject = $"[{severity}] {serviceName} Error",
            Body = BuildErrorBody(errorMessage, exception),
            SagaId = sagaId,
            Payload = BuildErrorPayload(errorMessage, exception, serviceName)
        }, cancellationToken);
    }

    /// <summary>
    /// Publishes a database connection error notification.
    /// </summary>
    public static Task<OutboundEventResponse> PublishDatabaseErrorAsync(
        this IRoutingServiceClient client,
        string serviceName,
        string clientId,
        string databaseName,
        string errorMessage,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        return client.PublishEventAsync(new OutboundEventRequest
        {
            Service = serviceName,
            Topic = "DatabaseConnectionError",
            ClientId = clientId,
            Severity = "Critical",
            Subject = $"[Critical] Database Connection Error - {databaseName}",
            Body = BuildErrorBody($"Database: {databaseName}\n{errorMessage}", exception),
            Payload = new Dictionary<string, object>
            {
                ["service_name"] = serviceName,
                ["database_name"] = databaseName,
                ["error_message"] = errorMessage,
                ["exception_type"] = exception?.GetType().Name ?? "Unknown",
                ["stack_trace"] = exception?.StackTrace ?? string.Empty,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Publishes an external service timeout notification.
    /// </summary>
    public static Task<OutboundEventResponse> PublishExternalTimeoutAsync(
        this IRoutingServiceClient client,
        string serviceName,
        string clientId,
        string externalServiceName,
        TimeSpan timeoutDuration,
        string? operation = null,
        CancellationToken cancellationToken = default)
    {
        var body = $"External service '{externalServiceName}' timed out after {timeoutDuration.TotalSeconds:F1} seconds.";
        if (!string.IsNullOrEmpty(operation))
        {
            body += $"\nOperation: {operation}";
        }

        return client.PublishEventAsync(new OutboundEventRequest
        {
            Service = serviceName,
            Topic = "ExternalServiceTimeout",
            ClientId = clientId,
            Severity = "Warning",
            Subject = $"[Warning] External Service Timeout - {externalServiceName}",
            Body = body,
            Payload = new Dictionary<string, object>
            {
                ["service_name"] = serviceName,
                ["external_service"] = externalServiceName,
                ["timeout_seconds"] = timeoutDuration.TotalSeconds,
                ["operation"] = operation ?? string.Empty,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Publishes an unhandled exception notification.
    /// </summary>
    public static Task<OutboundEventResponse> PublishUnhandledExceptionAsync(
        this IRoutingServiceClient client,
        string serviceName,
        string clientId,
        Exception exception,
        string? context = null,
        Guid? sagaId = null,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[Critical] Unhandled Exception in {serviceName}";
        var body = BuildErrorBody(context ?? "An unhandled exception occurred", exception);

        return client.PublishEventAsync(new OutboundEventRequest
        {
            Service = serviceName,
            Topic = "UnhandledException",
            ClientId = clientId,
            Severity = "Critical",
            Subject = subject,
            Body = body,
            SagaId = sagaId,
            Payload = BuildErrorPayload(context ?? "Unhandled exception", exception, serviceName)
        }, cancellationToken);
    }

    /// <summary>
    /// Publishes a service health degraded notification.
    /// </summary>
    public static Task<OutboundEventResponse> PublishHealthDegradedAsync(
        this IRoutingServiceClient client,
        string serviceName,
        string clientId,
        string reason,
        Dictionary<string, object>? healthMetrics = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["service_name"] = serviceName,
            ["reason"] = reason,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (healthMetrics != null)
        {
            foreach (var metric in healthMetrics)
            {
                payload[$"metric_{metric.Key}"] = metric.Value;
            }
        }

        return client.PublishEventAsync(new OutboundEventRequest
        {
            Service = serviceName,
            Topic = "ServiceHealthDegraded",
            ClientId = clientId,
            Severity = "Warning",
            Subject = $"[Warning] {serviceName} Health Degraded",
            Body = $"Service health has degraded.\n\nReason: {reason}",
            Payload = payload
        }, cancellationToken);
    }

    /// <summary>
    /// Publishes a service recovered notification.
    /// </summary>
    public static Task<OutboundEventResponse> PublishServiceRecoveredAsync(
        this IRoutingServiceClient client,
        string serviceName,
        string clientId,
        string? recoveryDetails = null,
        CancellationToken cancellationToken = default)
    {
        return client.PublishEventAsync(new OutboundEventRequest
        {
            Service = serviceName,
            Topic = "ServiceRecovered",
            ClientId = clientId,
            Severity = "Info",
            Subject = $"[Info] {serviceName} Recovered",
            Body = recoveryDetails ?? $"{serviceName} has recovered and is operating normally.",
            Payload = new Dictionary<string, object>
            {
                ["service_name"] = serviceName,
                ["recovery_time"] = DateTime.UtcNow.ToString("O")
            }
        }, cancellationToken);
    }

    private static string BuildErrorBody(string message, Exception? exception)
    {
        var body = message;

        if (exception != null)
        {
            body += $"\n\nException Type: {exception.GetType().FullName}";
            body += $"\nMessage: {exception.Message}";

            if (exception.InnerException != null)
            {
                body += $"\n\nInner Exception: {exception.InnerException.GetType().Name}";
                body += $"\nInner Message: {exception.InnerException.Message}";
            }

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                // Truncate stack trace if too long
                var stackTrace = exception.StackTrace;
                if (stackTrace.Length > 2000)
                {
                    stackTrace = stackTrace[..2000] + "\n... (truncated)";
                }
                body += $"\n\nStack Trace:\n{stackTrace}";
            }
        }

        return body;
    }

    private static Dictionary<string, object> BuildErrorPayload(
        string message,
        Exception? exception,
        string serviceName)
    {
        var payload = new Dictionary<string, object>
        {
            ["service_name"] = serviceName,
            ["error_message"] = message,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (exception != null)
        {
            payload["exception_type"] = exception.GetType().FullName ?? "Unknown";
            payload["exception_message"] = exception.Message;
            payload["stack_trace"] = exception.StackTrace ?? string.Empty;

            if (exception.InnerException != null)
            {
                payload["inner_exception_type"] = exception.InnerException.GetType().Name;
                payload["inner_exception_message"] = exception.InnerException.Message;
            }
        }

        return payload;
    }
}
