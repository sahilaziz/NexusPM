using Polly;
using Polly.Extensions.Http;

namespace Nexus.Infrastructure.Resilience;

/// <summary>
/// Enterprise Resilience Patterns
/// Circuit Breaker, Retry, Timeout
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry Policy - Transient xətalar üçün
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Circuit Breaker Policy
    /// 5 uğursuz cəhd → 30 saniyə fasilə
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// SQL Retry Policy
    /// </summary>
    public static IAsyncPolicy GetSqlRetryPolicy()
    {
        return Policy
            .Handle<Microsoft.Data.SqlClient.SqlException>(ex => 
                ex.IsTransient || ex.Number == 1205)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromMilliseconds(100 * retryAttempt));
    }

    /// <summary>
    /// Combined Policy
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy
            .WrapAsync(GetRetryPolicy(), GetCircuitBreakerPolicy());
    }
}
