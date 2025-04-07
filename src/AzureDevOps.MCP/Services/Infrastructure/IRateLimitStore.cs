namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Interface for rate limit storage backends.
/// </summary>
public interface IRateLimitStore
{
	Task<RateLimitStoreResult> CheckAndIncrementAsync (string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
	Task ResetAsync (string key, CancellationToken cancellationToken = default);
	Task<long> GetCurrentCountAsync (string key, CancellationToken cancellationToken = default);
}