using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.RateLimiting;

namespace Nexus.API.Security;

/// <summary>
/// Enterprise-grade security configurations for 5000+ users
/// </summary>
public static class EnterpriseSecurityExtensions
{
    public static IServiceCollection AddEnterpriseSecurity(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddSecurityHeaders();
        services.AddEnterpriseRateLimiting();
        services.AddCertificateAuthentication();
        services.AddAuditLogging();
        services.AddSingleton<ICertificateWhitelistService, CertificateWhitelistService>();
        
        return services;
    }

    private static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
    {
        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });
        return services;
    }

    public static IApplicationBuilder UseEnterpriseSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseHsts();
        
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'nonce-{nonce}'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";
            headers["Permissions-Policy"] = 
                "accelerometer=(), camera=(), geolocation=(), microphone=(), payment=()";
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            
            await next();
        });

        return app;
    }

    private static IServiceCollection AddEnterpriseRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // General API: 100 requests per minute per user
            options.AddPolicy("authenticated", context =>
            {
                var userId = context.User.Identity?.Name ?? 
                             context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                
                return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
            });

            // Login: 5 attempts per 5 minutes
            options.AddPolicy("login", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = "Too many requests. Please try again later.",
                    RetryAfter = 60
                }, token);
            };
        });

        return services;
    }

    private static IServiceCollection AddCertificateAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.Chained;
                options.RevocationMode = X509RevocationMode.Online;
                options.ValidateCertificateUse = true;
                options.ValidateValidityPeriod = true;
                
                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        var whitelistService = context.HttpContext.RequestServices
                            .GetRequiredService<ICertificateWhitelistService>();
                        
                        if (!whitelistService.IsWhitelisted(context.ClientCertificate.Thumbprint))
                        {
                            context.Fail("Certificate not whitelisted");
                            return Task.CompletedTask;
                        }
                        
                        context.Success();
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static IServiceCollection AddAuditLogging(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<AuditActionFilter>();
        });
        return services;
    }

    public static IApplicationBuilder UseIpFiltering(this IApplicationBuilder app, IConfiguration config)
    {
        var allowedIps = config.GetSection("Security:AllowedIPs").Get<string[]>();
        var blockedIps = config.GetSection("Security:BlockedIPs").Get<string[]>();

        if (allowedIps?.Length > 0 || blockedIps?.Length > 0)
        {
            app.Use(async (context, next) =>
            {
                var ip = context.Connection.RemoteIpAddress;
                
                if (blockedIps?.Any(b => IPAddress.Parse(b).Equals(ip)) == true)
                {
                    context.Response.StatusCode = 403;
                    return;
                }
                
                if (allowedIps?.Length > 0 && !allowedIps.Any(a => IPAddress.Parse(a).Equals(ip)))
                {
                    context.Response.StatusCode = 403;
                    return;
                }
                
                await next();
            });
        }

        return app;
    }
}

public interface IAuditLogger
{
    Task LogAsync(string action, string entityType, long? entityId, 
        Dictionary<string, object>? details = null);
}

public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public AuditLogger(ILogger<AuditLogger> logger, IHttpContextAccessor httpContext)
    {
        _logger = logger;
        _httpContext = httpContext;
    }

    public Task LogAsync(string action, string entityType, long? entityId, 
        Dictionary<string, object>? details = null)
    {
        var user = _httpContext.HttpContext?.User?.Identity?.Name ?? "anonymous";
        var ip = _httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        
        _logger.LogInformation(
            "AUDIT: User={User}, IP={IP}, Action={Action}, Entity={EntityType}:{EntityId}",
            user, ip, action, entityType, entityId);
        
        return Task.CompletedTask;
    }
}

public class AuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditLogger _auditLogger;

    public AuditActionFilter(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.DisplayName;
        await _auditLogger.LogAsync($"{actionName}_Start", "Action", null);
        await next();
    }
}

public interface ICertificateWhitelistService
{
    bool IsWhitelisted(string thumbprint);
}

public class CertificateWhitelistService : ICertificateWhitelistService
{
    private readonly ISet<string> _whitelist;

    public CertificateWhitelistService(IConfiguration configuration)
    {
        _whitelist = new HashSet<string>(
            configuration.GetSection("Security:WhitelistedCertificates").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsWhitelisted(string thumbprint)
    {
        return _whitelist.Contains(thumbprint);
    }
}
