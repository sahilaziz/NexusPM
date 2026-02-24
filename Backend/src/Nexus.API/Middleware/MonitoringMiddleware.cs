using System.Diagnostics;
using Nexus.Infrastructure.Monitoring;

namespace Nexus.API.Middleware;

/// <summary>
/// Monitoring Middleware - Request-ləri avtomatik izləyir
/// </summary>
public class MonitoringMiddleware
{
    private readonly RequestDelegate _next;

    public MonitoringMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPrivateMonitoringService monitoring)
    {
        // Health check və admin endpoint-lərini skip et
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/api/admin/monitoring"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
            stopwatch.Stop();
            
            // Log request
            await monitoring.LogRequestAsync(
                context, 
                stopwatch.ElapsedMilliseconds, 
                context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await monitoring.LogErrorAsync(ex, context);
            throw;
        }
    }
}
