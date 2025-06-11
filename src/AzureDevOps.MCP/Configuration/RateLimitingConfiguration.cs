using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Rate limiting configuration to prevent abuse.
/// </summary>
public class RateLimitingConfiguration
{
    /// <summary>
    /// Enable rate limiting (default: true).
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Requests per minute per client (default: 60).
    /// </summary>
    [Range(1, 1000)]
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Requests per hour per client (default: 1000).
    /// </summary>
    [Range(10, 10000)]
    public int RequestsPerHour { get; set; } = 1000;

    /// <summary>
    /// Requests per day per client (default: 10000).
    /// </summary>
    [Range(100, 100000)]
    public int RequestsPerDay { get; set; } = 10000;

    /// <summary>
    /// Client identification strategy (ip, user, api_key, combined).
    /// </summary>
    public string ClientIdentificationStrategy { get; set; } = "ip";

    /// <summary>
    /// Redis connection string for distributed rate limiting.
    /// </summary>
    public string RedisConnectionString { get; set; } = "";

    /// <summary>
    /// Enable distributed rate limiting across multiple instances.
    /// </summary>
    public bool EnableDistributedRateLimiting { get; set; } = false;

    /// <summary>
    /// Enable rate limiting by IP address.
    /// </summary>
    public bool EnableIpRateLimit { get; set; } = true;

    /// <summary>
    /// Enable rate limiting by API key.
    /// </summary>
    public bool EnableApiKeyRateLimit { get; set; } = true;

    /// <summary>
    /// Rate limit storage type (Memory, Redis).
    /// </summary>
    public string StorageType { get; set; } = "Memory";
}