using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Caching configuration for different cache layers.
/// </summary>
public class CachingConfiguration
{
	/// <summary>
	/// Enable in-memory caching (default: true).
	/// </summary>
	public bool EnableMemoryCache { get; set; } = true;

	/// <summary>
	/// Enable distributed caching with Redis (default: false).
	/// </summary>
	public bool EnableDistributedCache { get; set; } = false;

	/// <summary>
	/// Redis connection string for distributed caching.
	/// </summary>
	public string? RedisConnectionString { get; set; }

	/// <summary>
	/// Maximum memory cache size in MB (default: 100).
	/// </summary>
	[Range(10, 1000)]
	public int MaxMemoryCacheSizeMB { get; set; } = 100;

	/// <summary>
	/// Default cache expiration in minutes (default: 5).
	/// </summary>
	[Range(1, 1440)]
	public int DefaultExpirationMinutes { get; set; } = 5;

	/// <summary>
	/// Cache key prefix for this instance.
	/// </summary>
	public string KeyPrefix { get; set; } = "azdo-mcp";

	/// <summary>
	/// Enable cache statistics collection (default: true).
	/// </summary>
	public bool EnableStatistics { get; set; } = true;
}