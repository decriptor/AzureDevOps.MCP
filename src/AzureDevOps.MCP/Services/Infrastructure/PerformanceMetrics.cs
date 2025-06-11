namespace AzureDevOps.MCP.Services.Infrastructure;

// Modern record types for performance data structures
public record PerformanceMetrics(
    string OperationName,
    long CallCount,
    TimeSpan TotalResponseTime,
    TimeSpan AverageResponseTime,
    TimeSpan MinResponseTime,
    TimeSpan MaxResponseTime,
    long FailureCount,
    string? LastFailure,
    long AverageMemoryUsage,
    double CacheHitRate,
    DateTimeOffset LastUpdated
);