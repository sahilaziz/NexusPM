namespace Nexus.API.Security;

/// <summary>
/// Enterprise security headers middleware
/// Implements OWASP recommended security headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Security headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        
        // Content Security Policy
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self' wss: https:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // Strict Transport Security (HSTS)
        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Remove server identification
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");

        await _next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

/// <summary>
/// Anti-forgery token validation for state-changing operations
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ValidateAntiForgeryTokenAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        
        // Skip for GET, HEAD, OPTIONS, TRACE
        if (HttpMethods.IsGet(httpContext.Request.Method) ||
            HttpMethods.IsHead(httpContext.Request.Method) ||
            HttpMethods.IsOptions(httpContext.Request.Method) ||
            HttpMethods.IsTrace(httpContext.Request.Method))
        {
            await next();
            return;
        }

        // Validate token from header
        var requestToken = httpContext.Request.Headers["X-XSRF-Token"].ToString();
        var cookieToken = httpContext.Request.Cookies["XSRF-TOKEN"] ?? "";

        if (string.IsNullOrEmpty(requestToken) || requestToken != cookieToken)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                error = "Invalid anti-forgery token",
                message = "The request could not be validated. Please refresh the page and try again."
            });
            return;
        }

        await next();
    }
}

/// <summary>
/// Input sanitization helper
/// </summary>
public static class InputSanitizer
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    /// <summary>
    /// Sanitizes a file name to prevent path traversal attacks
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed";

        // Remove invalid characters
        foreach (var c in InvalidFileNameChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        // Prevent path traversal
        fileName = fileName.Replace("..", "_");
        fileName = fileName.Replace("/", "_");
        fileName = fileName.Replace("\\", "_");

        // Limit length
        if (fileName.Length > 255)
        {
            var extension = Path.GetExtension(fileName);
            fileName = fileName[..(255 - extension.Length)] + extension;
        }

        return fileName;
    }

    /// <summary>
    /// Sanitizes a path to prevent directory traversal
    /// </summary>
    public static string SanitizePath(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
            return basePath;

        // Combine and get full path
        var fullPath = Path.GetFullPath(Path.Combine(basePath, path));
        
        // Ensure it's within base path
        if (!fullPath.StartsWith(Path.GetFullPath(basePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException("Path traversal detected");
        }

        return fullPath;
    }

    /// <summary>
    /// Validates SQL input to prevent injection
    /// </summary>
    public static string SanitizeSqlInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Basic sanitization - in production use parameterized queries
        var dangerous = new[] { "--", ";", "/*", "*/", "xp_", "sp_", "EXEC", "EXECUTE", "DROP", "DELETE", "INSERT", "UPDATE" };
        
        foreach (var d in dangerous)
        {
            if (input.Contains(d, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Potentially dangerous SQL detected");
            }
        }

        return input;
    }
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}
