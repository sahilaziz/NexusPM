using System.Diagnostics;

namespace Nexus.API.Security;

/// <summary>
/// Enterprise request logging middleware for audit trails and telemetry
/// </summary>
public class EnterpriseRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnterpriseRequestLoggingMiddleware> _logger;
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "X-API-Key",
        "Cookie",
        "Password",
        "Token"
    };

    public EnterpriseRequestLoggingMiddleware(RequestDelegate next, ILogger<EnterpriseRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        // Pre-request logging
        var requestInfo = new RequestLogInfo
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            UserAgent = context.Request.Headers["User-Agent"].ToString(),
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User.Identity?.Name,
            Headers = GetSafeHeaders(context.Request.Headers)
        };

        _logger.LogInformation(
            "Request started: {RequestId} {Method} {Path} by {UserId} from {ClientIp}",
            requestId, requestInfo.Method, requestInfo.Path, requestInfo.UserId, requestInfo.ClientIp);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        Exception? exception = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            _logger.LogError(ex, "Request {RequestId} threw exception", requestId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Post-request logging
            var responseInfo = new ResponseLogInfo
            {
                RequestId = requestId,
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ContentLength = context.Response.ContentLength,
                ContentType = context.Response.ContentType
            };

            // Log based on status code
            if (exception != null)
            {
                _logger.LogError(
                    exception,
                    "Request {RequestId} failed: {Method} {Path} -> {StatusCode} in {DurationMs}ms",
                    requestId, requestInfo.Method, requestInfo.Path, responseInfo.StatusCode, responseInfo.DurationMs);
            }
            else if (context.Response.StatusCode >= 500)
            {
                _logger.LogError(
                    "Request {RequestId} server error: {Method} {Path} -> {StatusCode} in {DurationMs}ms",
                    requestId, requestInfo.Method, requestInfo.Path, responseInfo.StatusCode, responseInfo.DurationMs);
            }
            else if (context.Response.StatusCode >= 400)
            {
                _logger.LogWarning(
                    "Request {RequestId} client error: {Method} {Path} -> {StatusCode} in {DurationMs}ms",
                    requestId, requestInfo.Method, requestInfo.Path, responseInfo.StatusCode, responseInfo.DurationMs);
            }
            else if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Request {RequestId} slow: {Method} {Path} -> {StatusCode} in {DurationMs}ms",
                    requestId, requestInfo.Method, requestInfo.Path, responseInfo.StatusCode, responseInfo.DurationMs);
            }
            else
            {
                _logger.LogDebug(
                    "Request {RequestId} completed: {Method} {Path} -> {StatusCode} in {DurationMs}ms",
                    requestId, requestInfo.Method, requestInfo.Path, responseInfo.StatusCode, responseInfo.DurationMs);
            }

            // Write audit log for sensitive operations
            if (IsAuditOperation(context))
            {
                await WriteAuditLog(requestInfo, responseInfo);
            }
        }
    }

    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var header in headers)
        {
            if (SensitiveHeaders.Contains(header.Key))
            {
                safeHeaders[header.Key] = "[REDACTED]";
            }
            else
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }
        
        return safeHeaders;
    }

    private bool IsAuditOperation(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;
        
        // Audit: Document uploads, deletes, user management, permission changes
        return method is "POST" or "PUT" or "DELETE" &&
               (path.Contains("/documents") ||
                path.Contains("/users") ||
                path.Contains("/auth") ||
                path.Contains("/admin"));
    }

    private Task WriteAuditLog(RequestLogInfo request, ResponseLogInfo response)
    {
        // In production, this would write to a separate audit log table
        _logger.LogInformation(
            "AUDIT: {RequestId} {Method} {Path} by {UserId} -> {StatusCode} at {Timestamp}",
            request.RequestId, request.Method, request.Path, request.UserId, response.StatusCode, request.Timestamp);
        
        return Task.CompletedTask;
    }
}

public class RequestLogInfo
{
    public string RequestId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string QueryString { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string? ClientIp { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class ResponseLogInfo
{
    public string RequestId { get; set; } = "";
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public long? ContentLength { get; set; }
    public string? ContentType { get; set; }
}

public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseEnterpriseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EnterpriseRequestLoggingMiddleware>();
    }
}
