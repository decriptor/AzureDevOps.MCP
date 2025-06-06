namespace AzureDevOps.MCP.Services.Infrastructure;

public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string identifier, CancellationToken cancellationToken = default);
    Task<RateLimitStatus> GetStatusAsync(string identifier);
    void Reset(string identifier);
    void ResetAll();
}

public class RateLimitStatus
{
    public int RequestsRemaining { get; init; }
    public int RequestsPerWindow { get; init; }
    public TimeSpan WindowSize { get; init; }
    public DateTime WindowStart { get; init; }
    public DateTime? NextReset { get; init; }
}

public class RateLimitOptions
{
    public int RequestsPerWindow { get; set; } = 100;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public bool ThrowOnLimit { get; set; } = false;
}

public class RateLimitExceededException : Exception
{
    public RateLimitStatus Status { get; }

    public RateLimitExceededException(RateLimitStatus status) 
        : base($"Rate limit exceeded. {status.RequestsRemaining} requests remaining in window.")
    {
        Status = status;
    }
}

public interface IHealthCheckService
{
    Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, HealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default);
    void RegisterCheck(string name, Func<CancellationToken, Task<HealthStatus>> check);
    event EventHandler<HealthChangedEventArgs>? HealthChanged;
}

public class HealthStatus
{
    public bool IsHealthy { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object>? Data { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public Exception? Exception { get; init; }

    public static HealthStatus Healthy(string? description = null, Dictionary<string, object>? data = null, TimeSpan? responseTime = null)
        => new() { IsHealthy = true, Description = description, Data = data, ResponseTime = responseTime };

    public static HealthStatus Unhealthy(string? description = null, Exception? exception = null, Dictionary<string, object>? data = null, TimeSpan? responseTime = null)
        => new() { IsHealthy = false, Description = description, Exception = exception, Data = data, ResponseTime = responseTime };
}

public class HealthChangedEventArgs : EventArgs
{
    public string CheckName { get; }
    public HealthStatus Status { get; }
    public HealthStatus? PreviousStatus { get; }

    public HealthChangedEventArgs(string checkName, HealthStatus status, HealthStatus? previousStatus = null)
    {
        CheckName = checkName;
        Status = status;
        PreviousStatus = previousStatus;
    }
}