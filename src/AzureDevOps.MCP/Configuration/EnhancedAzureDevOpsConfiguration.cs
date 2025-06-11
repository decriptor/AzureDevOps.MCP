using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Enhanced Azure DevOps configuration with production features.
/// </summary>
public class EnhancedAzureDevOpsConfiguration : AzureDevOpsConfiguration
{
    /// <summary>
    /// Connection timeout in seconds (default: 30).
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Request timeout in seconds (default: 60).
    /// </summary>
    [Range(10, 600)]
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum retry attempts for failed requests (default: 3).
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base retry delay in milliseconds (default: 1000).
    /// </summary>
    [Range(100, 10000)]
    public int BaseRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum concurrent requests per organization (default: 10).
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Enable request compression (default: true).
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// User agent string for API requests.
    /// </summary>
    public string UserAgent { get; set; } = "AzureDevOps-MCP/1.0";
}