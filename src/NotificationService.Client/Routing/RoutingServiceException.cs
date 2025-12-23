namespace NotificationService.Client.Routing;

/// <summary>
/// Exception thrown when routing service operations fail.
/// </summary>
public class RoutingServiceException : Exception
{
    public int? StatusCode { get; }
    public string? ResponseBody { get; }

    public RoutingServiceException(string message) : base(message) { }

    public RoutingServiceException(string message, Exception innerException)
        : base(message, innerException) { }

    public RoutingServiceException(string message, int statusCode, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
