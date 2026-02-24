using System.Threading.RateLimiting;

namespace Nexus.API.Security;

/// <summary>
/// Enterprise rate limiting configuration for 5000+ users
/// Per-user, per-endpoint, and global rate limiting
/// </summary>
public static class RateLimitingConfig
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limit (all endpoints combined)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1000,        // 1000 requests
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 50           // Queue up to 50 requests when limit reached
                });
            });

            // Per-user rate limit (authenticated users)
            options.AddPolicy("per-user", context =>
            {
                var userId = context.User.Identity?.Name ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 500,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                });
            });

            // Strict rate limit for authentication endpoints
            options.AddPolicy("auth", context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,          // 10 login attempts
                    Window = TimeSpan.FromMinutes(5),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0             // No queue for auth
                });
            });

            // Rate limit for file uploads (larger limits due to file processing)
            options.AddPolicy("upload", context =>
            {
                var userId = context.User.Identity?.Name ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 50,          // 50 uploads
                    Window = TimeSpan.FromMinutes(10),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
            });

            // Rate limit for search endpoints (computationally expensive)
            options.AddPolicy("search", context =>
            {
                var userId = context.User.Identity?.Name ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,         // 100 searches
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                });
            });

            // Token bucket for burst traffic handling
            options.AddPolicy("burst", context =>
            {
                var userId = context.User.Identity?.Name ?? "anonymous";
                return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = 10,
                    AutoReplenishment = true
                });
            });

            // On rejected response
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterTimeSpan)
                    ? retryAfterTimeSpan.TotalSeconds
                    : 60;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = Math.Ceiling(retryAfter),
                    timestamp = DateTime.UtcNow
                }, token);
            };

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}

/// <summary>
/// Rate limiting attributes for controllers
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RateLimitAttribute : Attribute
{
    public string Policy { get; }
    
    public RateLimitAttribute(string policy)
    {
        Policy = policy;
    }
}
