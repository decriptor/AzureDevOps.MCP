namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Interface for rate limiting functionality.
/// </summary>
public interface IRateLimiter
{
	/// <summary>
	/// Attempts to acquire a request slot for the specified identifier.
	/// </summary>
	/// <param name="identifier">The identifier to check rate limits for</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if the request is allowed, false if rate limited</returns>
	Task<bool> TryAcquireAsync(string identifier, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the current rate limit status for the specified identifier.
	/// </summary>
	/// <param name="identifier">The identifier to check</param>
	/// <returns>The current rate limit status</returns>
	Task<RateLimitStatus> GetStatusAsync(string identifier);

	/// <summary>
	/// Resets the rate limit for the specified identifier.
	/// </summary>
	/// <param name="identifier">The identifier to reset</param>
	void Reset(string identifier);

	/// <summary>
	/// Resets all rate limits.
	/// </summary>
	void ResetAll();
}