namespace AzureDevOps.MCP.Services;

public interface ICacheService
{
	Task<T?> GetAsync<T> (string key, CancellationToken cancellationToken = default) where T : class;
	Task SetAsync<T> (string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
	Task<T> GetOrSetAsync<T> (string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
	Task RemoveAsync (string key, CancellationToken cancellationToken = default);
	Task RemoveByPatternAsync (string pattern, CancellationToken cancellationToken = default);
	Task ClearAsync (CancellationToken cancellationToken = default);
	CacheStatistics GetStatistics ();
	Task<bool> ExistsAsync (string key, CancellationToken cancellationToken = default);
}