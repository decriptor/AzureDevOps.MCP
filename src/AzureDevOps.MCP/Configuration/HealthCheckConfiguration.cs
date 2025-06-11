using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Health check configuration for monitoring and alerting.
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Enable health checks (default: true).
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check endpoint path (default: /health).
    /// </summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Enable detailed health check responses.
    /// </summary>
    public bool EnableDetailedResponse { get; set; } = false;

    /// <summary>
    /// Health check timeout in seconds (default: 30).
    /// </summary>
    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable Azure DevOps connectivity check.
    /// </summary>
    public bool EnableAzureDevOpsCheck { get; set; } = true;

    /// <summary>
    /// Enable cache connectivity check.
    /// </summary>
    public bool EnableCacheCheck { get; set; } = true;

    /// <summary>
    /// Enable database connectivity check (if applicable).
    /// </summary>
    public bool EnableDatabaseCheck { get; set; } = false;

    /// <summary>
    /// Enable memory usage check.
    /// </summary>
    public bool EnableMemoryCheck { get; set; } = true;

    /// <summary>
    /// Memory usage threshold for health check (default: 90%).
    /// </summary>
    [Range(50, 95)]
    public int MemoryThresholdPercent { get; set; } = 90;
}