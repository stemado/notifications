using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Client.Routing.Models;

namespace NotificationService.Client.Routing;

/// <summary>
/// HTTP client implementation for the NotificationService routing API.
/// </summary>
public class RoutingServiceClient : IRoutingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RoutingServiceClient> _logger;
    private readonly RoutingServiceOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RoutingServiceClient(
        HttpClient httpClient,
        ILogger<RoutingServiceClient> logger,
        IOptions<RoutingServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
    }

    public async Task<OutboundEventResponse> PublishEventAsync(
        OutboundEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug(
                "Routing is disabled. Skipping publish for {Service}/{Topic}",
                request.Service, request.Topic);
            return new OutboundEventResponse
            {
                EventId = Guid.Empty,
                DeliveryCount = 0,
                HasMatchingPolicies = false,
                Message = "Routing is disabled"
            };
        }

        _logger.LogInformation(
            "Publishing outbound event: {Service}/{Topic} for client {ClientId}",
            request.Service, request.Topic, request.ClientId);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/routing/events",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to publish outbound event: {StatusCode} - {Body}",
                    (int)response.StatusCode, body);

                throw new RoutingServiceException(
                    $"Failed to publish outbound event: {response.StatusCode}",
                    (int)response.StatusCode,
                    body);
            }

            var result = await response.Content.ReadFromJsonAsync<OutboundEventResponse>(
                JsonOptions, cancellationToken);

            if (result == null)
            {
                throw new RoutingServiceException("Received null response from routing service");
            }

            _logger.LogInformation(
                "Published outbound event {EventId} with {DeliveryCount} deliveries",
                result.EventId, result.DeliveryCount);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error publishing outbound event");
            throw new RoutingServiceException("Failed to connect to routing service", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout publishing outbound event");
            throw new RoutingServiceException("Routing service request timed out", ex);
        }
    }

    public async Task<ClientRoutingConfiguration> GetClientRoutingAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting routing configuration for client {ClientId}", clientId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"api/routing/clients/{Uri.EscapeDataString(clientId)}/configuration",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                // Return empty config for 404 (no policies configured yet)
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("No routing configuration found for client {ClientId}", clientId);
                    return new ClientRoutingConfiguration { ClientId = clientId };
                }

                throw new RoutingServiceException(
                    $"Failed to get client routing: {response.StatusCode}",
                    (int)response.StatusCode,
                    body);
            }

            var result = await response.Content.ReadFromJsonAsync<ClientRoutingConfiguration>(
                JsonOptions, cancellationToken);

            return result ?? new ClientRoutingConfiguration { ClientId = clientId };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting client routing for {ClientId}", clientId);
            throw new RoutingServiceException("Failed to connect to routing service", ex);
        }
    }

    public async Task<List<RoutingPolicySummary>> GetMatchingPoliciesAsync(
        string service,
        string topic,
        string? clientId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting matching policies for {Service}/{Topic} client={ClientId}",
            service, topic, clientId ?? "(any)");

        try
        {
            var url = $"api/routing/policies/match?service={Uri.EscapeDataString(service)}&topic={Uri.EscapeDataString(topic)}";
            if (!string.IsNullOrEmpty(clientId))
            {
                url += $"&clientId={Uri.EscapeDataString(clientId)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new RoutingServiceException(
                    $"Failed to get matching policies: {response.StatusCode}",
                    (int)response.StatusCode,
                    body);
            }

            var result = await response.Content.ReadFromJsonAsync<List<RoutingPolicySummary>>(
                JsonOptions, cancellationToken);

            return result ?? new List<RoutingPolicySummary>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting matching policies");
            throw new RoutingServiceException("Failed to connect to routing service", ex);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Routing service health check failed");
            return false;
        }
    }
}
