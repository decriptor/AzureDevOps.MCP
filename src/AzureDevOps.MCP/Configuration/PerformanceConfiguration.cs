using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Performance monitoring and optimization configuration.
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Enable performance monitoring (default: true).
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Slow operation threshold in milliseconds (default: 1000).
    /// </summary>
    [Range(100, 60000)]
    public int SlowOperationThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Very slow operation threshold in milliseconds (default: 5000).
    /// </summary>
    [Range(1000, 300000)]
    public int VerySlowOperationThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Maximum operation history entries to keep (default: 1000).
    /// </summary>
    [Range(100, 10000)]
    public int MaxOperationHistoryEntries { get; set; } = 1000;

    /// <summary>
    /// Enable memory pressure monitoring.
    /// </summary>
    public bool EnableMemoryPressureMonitoring { get; set; } = true;

    /// <summary>
    /// Memory pressure threshold percentage (default: 80).
    /// </summary>
    [Range(50, 95)]
    public int MemoryPressureThresholdPercent { get; set; } = 80;

    /// <summary>
    /// Enable circuit breaker pattern.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Circuit breaker failure threshold (default: 5).
    /// </summary>
    [Range(3, 20)]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker timeout in seconds (default: 60).
    /// </summary>
    [Range(30, 600)]
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;
}