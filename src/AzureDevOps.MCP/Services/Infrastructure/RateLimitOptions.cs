namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitOptions
{
	/// <summary>
	/// The number of requests allowed per window. Default is 100.
	/// </summary>
	public int RequestsPerWindow { get; set; } = 100;

	/// <summary>
	/// The size of the rate limiting window. Default is 1 minute.
	/// </summary>
	public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Whether to throw an exception when rate limit is exceeded. Default is false.
	/// </summary>
	public bool ThrowOnLimit { get; set; } = false;
}