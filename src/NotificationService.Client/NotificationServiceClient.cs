using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Client.Configuration;
using NotificationService.Client.Events;
using NotificationService.Client.Models;

namespace NotificationService.Client;

/// <summary>
/// HTTP client implementation for the NotificationService API.
/// Uses Polly for resilience (retry, circuit breaker, timeout).
/// Throws on failure - no silent fallbacks.
/// </summary>
public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;
    private readonly NotificationServiceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    // System user ID for service-to-service notifications
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public NotificationServiceClient(
        HttpClient httpClient,
        IOptions<NotificationServiceOptions> options,
        ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    #region Core Notification Operations

    public async Task<NotificationResponse> CreateNotificationAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Creating notification: Title={Title}, Severity={Severity}",
            request.Title, request.Severity);

        var response = await PostAsync<CreateNotificationRequest, NotificationApiResponse>(
            "/api/notifications",
            request,
            cancellationToken);

        return MapToNotificationResponse(response, false);
    }

    public async Task<NotificationResponse> CreateOrUpdateNotificationAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.GroupKey))
        {
            throw new ArgumentException("GroupKey is required for CreateOrUpdate operations", nameof(request));
        }

        _logger.LogDebug("Creating/updating notification: GroupKey={GroupKey}, Title={Title}",
            request.GroupKey, request.Title);

        var response = await PostAsync<CreateNotificationRequest, NotificationApiResponse>(
            "/api/notifications/create-or-update",
            request,
            cancellationToken);

        return MapToNotificationResponse(response, response?.WasUpdated ?? false);
    }

    public async Task AcknowledgeNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Acknowledging notification: {NotificationId} by user {UserId}",
            notificationId, userId);

        await PostAsync<object, object>(
            $"/api/notifications/{notificationId}/acknowledge?userId={userId}",
            new { },
            cancellationToken);
    }

    public async Task DismissNotificationAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Dismissing notification: {NotificationId} by user {UserId}",
            notificationId, userId);

        await PostAsync<object, object>(
            $"/api/notifications/{notificationId}/dismiss?userId={userId}",
            new { },
            cancellationToken);
    }

    #endregion

    #region Event Publishing

    public async Task<NotificationResponse> PublishSagaStuckEventAsync(
        SagaStuckEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogInformation("Publishing SagaStuckEvent: SagaId={SagaId}, ClientId={ClientId}, Duration={Duration}",
            evt.SagaId, evt.ClientId, evt.StuckDuration);

        var severity = evt.StuckDuration.TotalHours >= 24 ? NotificationSeverity.Critical
            : evt.StuckDuration.TotalHours >= 4 ? NotificationSeverity.Urgent
            : NotificationSeverity.Warning;

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = severity,
            Title = $"Workflow Stuck: {evt.ClientName}",
            Message = $"Workflow {evt.SagaId} has been stuck in {evt.CurrentState} state for {FormatDuration(evt.StuckDuration)}. File: {evt.FileName ?? "N/A"}",
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(SagaStuckEvent),
            GroupKey = $"saga:stuck:{evt.SagaId}",
            RequiresAck = true,
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["currentState"] = evt.CurrentState,
                ["stuckDurationMinutes"] = evt.StuckDuration.TotalMinutes,
                ["fileName"] = evt.FileName ?? string.Empty,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Workflow", Url = $"/workflows/{evt.SagaId}", ActionType = "link" },
                new() { Label = "Force Retry", Url = $"/api/workflows/{evt.SagaId}/retry", ActionType = "action" }
            }
        };

        return await CreateOrUpdateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishImportCompletedEventAsync(
        ImportCompletedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogInformation("Publishing ImportCompletedEvent: SagaId={SagaId}, ClientId={ClientId}, Records={TotalRecords}",
            evt.SagaId, evt.ClientId, evt.TotalRecords);

        var severity = evt.FailureCount > 0 ? NotificationSeverity.Warning : NotificationSeverity.Info;

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = severity,
            Title = $"Import Completed: {evt.ClientName}",
            Message = $"File '{evt.FileName}' processed successfully. {evt.SuccessCount}/{evt.TotalRecords} records imported. " +
                      $"New hires: {evt.NewHireCount}, Terminations: {evt.TerminationCount}, Demographics: {evt.DemographicChangeCount}. " +
                      $"Duration: {FormatDuration(evt.Duration)}",
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(ImportCompletedEvent),
            GroupKey = $"import:completed:{evt.SagaId}",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["fileName"] = evt.FileName,
                ["totalRecords"] = evt.TotalRecords,
                ["successCount"] = evt.SuccessCount,
                ["failureCount"] = evt.FailureCount,
                ["skippedCount"] = evt.SkippedCount,
                ["newHireCount"] = evt.NewHireCount,
                ["terminationCount"] = evt.TerminationCount,
                ["demographicChangeCount"] = evt.DemographicChangeCount,
                ["durationSeconds"] = evt.Duration.TotalSeconds,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Details", Url = $"/imports/{evt.SagaId}", ActionType = "link" }
            }
        };

        return await CreateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishImportFailedEventAsync(
        ImportFailedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogWarning("Publishing ImportFailedEvent: SagaId={SagaId}, ClientId={ClientId}, Error={ErrorMessage}",
            evt.SagaId, evt.ClientId, evt.ErrorMessage);

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = evt.WasEscalated ? NotificationSeverity.Critical : NotificationSeverity.Urgent,
            Title = $"Import Failed: {evt.ClientName}",
            Message = $"File '{evt.FileName}' failed to import at state '{evt.FailedAtState}'. Error: {evt.ErrorMessage}",
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(ImportFailedEvent),
            GroupKey = $"import:failed:{evt.SagaId}",
            RequiresAck = true,
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["fileName"] = evt.FileName,
                ["errorMessage"] = evt.ErrorMessage,
                ["exceptionType"] = evt.ExceptionType ?? string.Empty,
                ["failedAtState"] = evt.FailedAtState,
                ["retryCount"] = evt.RetryCount,
                ["wasEscalated"] = evt.WasEscalated,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Error", Url = $"/imports/{evt.SagaId}/errors", ActionType = "link" },
                new() { Label = "Retry Import", Url = $"/api/imports/{evt.SagaId}/retry", ActionType = "action" }
            }
        };

        return await CreateOrUpdateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishEscalationCreatedEventAsync(
        EscalationCreatedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogWarning("Publishing EscalationCreatedEvent: EscalationId={EscalationId}, Type={Type}, Severity={Severity}",
            evt.EscalationId, evt.EscalationType, evt.Severity);

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = evt.Severity,
            Title = $"Escalation: {evt.EscalationType} - {evt.ClientName}",
            Message = evt.Reason,
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventId = evt.EscalationId,
            EventType = nameof(EscalationCreatedEvent),
            GroupKey = $"escalation:{evt.EscalationId}",
            RequiresAck = true,
            RepeatInterval = evt.Severity >= NotificationSeverity.Urgent ? 30 : null, // Repeat every 30 min for urgent+
            Metadata = new Dictionary<string, object>
            {
                ["escalationId"] = evt.EscalationId,
                ["escalationType"] = evt.EscalationType,
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["currentState"] = evt.CurrentState,
                ["timeInStateMinutes"] = evt.TimeInState.TotalMinutes,
                ["fileName"] = evt.FileName ?? string.Empty,
                ["suggestedActions"] = evt.SuggestedActions,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Escalation", Url = $"/escalations/{evt.EscalationId}", ActionType = "link" },
                new() { Label = "Resolve", Url = $"/api/escalations/{evt.EscalationId}/resolve", ActionType = "action" }
            }
        };

        return await CreateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishFileProcessingErrorEventAsync(
        FileProcessingErrorEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogWarning("Publishing FileProcessingErrorEvent: ClientId={ClientId}, ErrorType={ErrorType}",
            evt.ClientId, evt.ErrorType);

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = evt.Severity,
            Title = $"File Error: {evt.ErrorType} - {evt.ClientName}",
            Message = $"Error processing file '{evt.FilePath}': {evt.ErrorMessage}",
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(FileProcessingErrorEvent),
            GroupKey = $"file:error:{evt.ClientId}:{evt.ErrorType}:{DateTime.UtcNow:yyyyMMddHH}",
            RequiresAck = !evt.IsRecoverable,
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["filePath"] = evt.FilePath,
                ["errorType"] = evt.ErrorType,
                ["errorMessage"] = evt.ErrorMessage,
                ["isRecoverable"] = evt.IsRecoverable,
                ["resolution"] = evt.Resolution ?? string.Empty
            }
        };

        return await CreateOrUpdateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishSLABreachEventAsync(
        SLABreachEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogWarning("Publishing SLABreachEvent: SagaId={SagaId}, ClientId={ClientId}, SLAType={SLAType}, Actual={ActualMinutes}min vs Threshold={ThresholdMinutes}min",
            evt.SagaId, evt.ClientId, evt.SLAType, evt.ActualMinutes, evt.ThresholdMinutes);

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = evt.Severity,
            Title = $"SLA Breach: {evt.SLAType} - {evt.ClientName}",
            Message = $"Workflow {evt.SagaId} exceeded SLA threshold. Actual: {evt.ActualMinutes} minutes vs Threshold: {evt.ThresholdMinutes} minutes. Current state: {evt.CurrentState}",
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(SLABreachEvent),
            GroupKey = $"sla:breach:{evt.SagaId}:{evt.SLAType}",
            RequiresAck = evt.Severity >= NotificationSeverity.Urgent,
            RepeatInterval = evt.Severity >= NotificationSeverity.Critical ? 15 : null,
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["slaType"] = evt.SLAType,
                ["thresholdMinutes"] = evt.ThresholdMinutes,
                ["actualMinutes"] = evt.ActualMinutes,
                ["currentState"] = evt.CurrentState,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Workflow", Url = $"/workflows/{evt.SagaId}", ActionType = "link" }
            }
        };

        return await CreateOrUpdateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishPlanSourceOperationFailedEventAsync(
        PlanSourceOperationFailedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogWarning("Publishing PlanSourceOperationFailedEvent: SagaId={SagaId}, ClientId={ClientId}, Operation={Operation}, IsRetryable={IsRetryable}",
            evt.SagaId, evt.ClientId, evt.OperationType, evt.IsRetryable);

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = evt.Severity,
            Title = $"PlanSource {evt.OperationType} Failed: {evt.ClientName}",
            Message = $"PlanSource operation '{evt.OperationType}' failed for workflow {evt.SagaId}. " +
                      $"Error: {evt.ErrorMessage}. " +
                      $"Attempt {evt.AttemptNumber}/{evt.MaxRetries}. " +
                      (evt.IsRetryable ? "Will retry automatically." : "Non-retryable - requires manual intervention."),
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(PlanSourceOperationFailedEvent),
            GroupKey = $"plansource:failed:{evt.SagaId}:{evt.OperationType}",
            RequiresAck = !evt.IsRetryable,
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["operationType"] = evt.OperationType,
                ["errorMessage"] = evt.ErrorMessage,
                ["errorCode"] = evt.ErrorCode ?? string.Empty,
                ["isRetryable"] = evt.IsRetryable,
                ["attemptNumber"] = evt.AttemptNumber,
                ["maxRetries"] = evt.MaxRetries,
                ["currentState"] = evt.CurrentState,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Workflow", Url = $"/workflows/{evt.SagaId}", ActionType = "link" },
                new() { Label = "Force Retry", Url = $"/api/workflows/{evt.SagaId}/retry", ActionType = "action" }
            }
        };

        return await CreateOrUpdateNotificationAsync(request, cancellationToken);
    }

    public async Task<NotificationResponse> PublishAggregateGenerationStalledEventAsync(
        AggregateGenerationStalledEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        _logger.LogWarning("Publishing AggregateGenerationStalledEvent: SagaId={SagaId}, ClientId={ClientId}, WaitCount={WaitCount}/{MaxWait}, MinutesWaiting={Minutes}",
            evt.SagaId, evt.ClientId, evt.WaitCount, evt.MaxWaitCount, evt.MinutesWaiting);

        var request = new CreateNotificationRequest
        {
            UserId = SystemUserId,
            TenantId = evt.TenantId,
            Severity = evt.Severity,
            Title = $"Aggregate Stalled: {evt.ClientName}",
            Message = $"Aggregate generation appears stalled for workflow {evt.SagaId}. " +
                      $"Checked {evt.WaitCount}/{evt.MaxWaitCount} times over {evt.MinutesWaiting} minutes. " +
                      (evt.FileName != null ? $"File: {evt.FileName}" : ""),
            SagaId = evt.SagaId,
            ClientId = ParseGuidOrDefault(evt.ClientId),
            EventType = nameof(AggregateGenerationStalledEvent),
            GroupKey = $"aggregate:stalled:{evt.SagaId}",
            RequiresAck = evt.WaitCount >= evt.MaxWaitCount,
            Metadata = new Dictionary<string, object>
            {
                ["clientId"] = evt.ClientId,
                ["clientName"] = evt.ClientName,
                ["waitCount"] = evt.WaitCount,
                ["maxWaitCount"] = evt.MaxWaitCount,
                ["minutesWaiting"] = evt.MinutesWaiting,
                ["fileName"] = evt.FileName ?? string.Empty,
                ["correlationId"] = evt.CorrelationId ?? string.Empty
            },
            Actions = new List<NotificationAction>
            {
                new() { Label = "View Workflow", Url = $"/workflows/{evt.SagaId}", ActionType = "link" },
                new() { Label = "Check Aggregate", Url = $"/api/workflows/{evt.SagaId}/check-aggregate", ActionType = "action" }
            }
        };

        return await CreateOrUpdateNotificationAsync(request, cancellationToken);
    }

    #endregion

    #region Health Check

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotificationService health check failed");
            return false;
        }
    }

    #endregion

    #region Private Helpers

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        string? responseBody = null;

        try
        {
            response = await _httpClient.PostAsJsonAsync(requestUri, request, _jsonOptions, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new NotificationServiceException(
                    $"NotificationService request failed: {response.StatusCode}",
                    (int)response.StatusCode,
                    responseBody,
                    requestUri);
            }

            if (string.IsNullOrEmpty(responseBody))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);
        }
        catch (NotificationServiceException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to NotificationService failed: {RequestUri}", requestUri);
            throw new NotificationServiceException(
                $"HTTP request to NotificationService failed: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "NotificationService request timed out: {RequestUri}", requestUri);
            throw new NotificationServiceException(
                $"NotificationService request timed out after {_options.TimeoutSeconds} seconds",
                ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize NotificationService response: {ResponseBody}", responseBody);
            throw new NotificationServiceException(
                $"Failed to deserialize NotificationService response: {ex.Message}",
                (int)(response?.StatusCode ?? 0),
                responseBody,
                requestUri,
                ex);
        }
    }

    private static NotificationResponse MapToNotificationResponse(NotificationApiResponse? apiResponse, bool wasUpdated)
    {
        if (apiResponse == null)
        {
            return NotificationResponse.Failed("Empty response from NotificationService");
        }

        return new NotificationResponse
        {
            Success = true,
            NotificationId = apiResponse.Id,
            StatusCode = wasUpdated ? 200 : 201,
            WasUpdated = wasUpdated,
            Timestamp = apiResponse.CreatedAt
        };
    }

    private static Guid? ParseGuidOrDefault(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{duration.TotalDays:F1} days";
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1} hours";
        return $"{duration.TotalMinutes:F0} minutes";
    }

    #endregion

    #region Internal Response Models

    private class NotificationApiResponse
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool WasUpdated { get; set; }
    }

    #endregion
}
