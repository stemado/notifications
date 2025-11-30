namespace NotificationService.Client;

/// <summary>
/// Exception thrown when NotificationService operations fail.
/// Contains details about the HTTP status code and response.
/// </summary>
public class NotificationServiceException : Exception
{
    /// <summary>
    /// HTTP status code from the failed request
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Response body from the failed request (if available)
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Whether this error is potentially retryable
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Request URI that failed
    /// </summary>
    public string? RequestUri { get; }

    public NotificationServiceException(string message)
        : base(message)
    {
    }

    public NotificationServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
        IsRetryable = innerException is HttpRequestException or TaskCanceledException;
    }

    public NotificationServiceException(
        string message,
        int statusCode,
        string? responseBody = null,
        string? requestUri = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        RequestUri = requestUri;
        IsRetryable = statusCode >= 500 || statusCode == 429;
    }

    public NotificationServiceException(
        string message,
        int statusCode,
        string? responseBody,
        string? requestUri,
        Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        RequestUri = requestUri;
        IsRetryable = statusCode >= 500 || statusCode == 429;
    }

    public override string ToString()
    {
        var details = new List<string> { base.ToString() };

        if (StatusCode.HasValue)
            details.Add($"StatusCode: {StatusCode}");

        if (!string.IsNullOrEmpty(RequestUri))
            details.Add($"RequestUri: {RequestUri}");

        if (!string.IsNullOrEmpty(ResponseBody))
            details.Add($"ResponseBody: {ResponseBody}");

        return string.Join(Environment.NewLine, details);
    }
}
