namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Represents the current status of rate limiting for an identifier.
/// </summary>
public class RateLimitStatus
{
	/// <summary>
	/// The number of requests remaining in the current window.
	/// </summary>
	public int RequestsRemaining { get; init; }

	/// <summary>
	/// The total number of requests allowed per window.
	/// </summary>
	public int RequestsPerWindow { get; init; }

	/// <summary>
	/// The size of the rate limiting window.
	/// </summary>
	public TimeSpan WindowSize { get; init; }

	/// <summary>
	/// When the current window started.
	/// </summary>
	public DateTime WindowStart { get; init; }

	/// <summary>
	/// When the rate limit will reset (if applicable).
	/// </summary>
	public DateTime? NextReset { get; init; }
}