using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AzureDevOps.MCP.Services;

public class CacheService : ICacheService
{
	readonly IMemoryCache _cache;
	readonly ILogger<CacheService> _logger;
	readonly HashSet<string> _keys = new ();
	readonly object _keysLock = new ();

	public CacheService (IMemoryCache cache, ILogger<CacheService> logger)
	{
		_cache = cache;
		_logger = logger;
	}

	public Task<T?> GetAsync<T> (string key, CancellationToken cancellationToken = default) where T : class
	{
		try {
			if (_cache.TryGetValue (key, out T? value)) {
				_logger.LogDebug ("Cache hit for key: {Key}", key);
				return Task.FromResult (value);
			}

			_logger.LogDebug ("Cache miss for key: {Key}", key);
			return Task.FromResult<T?> (null);
		} catch (Exception ex) {
			_logger.LogError (ex, "Error retrieving key {Key} from cache", key);
			return Task.FromResult<T?> (null);
		}
	}

	public Task SetAsync<T> (string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
	{
		try {
			var options = new MemoryCacheEntryOptions ();

			if (expiration.HasValue) {
				options.SetAbsoluteExpiration (expiration.Value);
			} else {
				// Default expiration of 5 minutes
				options.SetAbsoluteExpiration (TimeSpan.FromMinutes (5));
			}

			options.RegisterPostEvictionCallback ((evictedKey, evictedValue, reason, state) => {
				lock (_keysLock) {
					_keys.Remove (evictedKey.ToString ()!);
				}
			});

			_cache.Set (key, value, options);

			lock (_keysLock) {
				_keys.Add (key);
			}

			_logger.LogDebug ("Cached key: {Key} with expiration: {Expiration}", key, expiration);
			return Task.CompletedTask;
		} catch (Exception ex) {
			_logger.LogError (ex, "Error setting key {Key} in cache", key);
			return Task.CompletedTask;
		}
	}

	public Task RemoveAsync (string key, CancellationToken cancellationToken = default)
	{
		try {
			_cache.Remove (key);

			lock (_keysLock) {
				_keys.Remove (key);
			}

			_logger.LogDebug ("Removed key from cache: {Key}", key);
			return Task.CompletedTask;
		} catch (Exception ex) {
			_logger.LogError (ex, "Error removing key {Key} from cache", key);
			return Task.CompletedTask;
		}
	}

	public async Task<T> GetOrSetAsync<T> (string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
	{
		var cached = await GetAsync<T> (key);
		if (cached != null) {
			return cached;
		}

		var value = await factory (cancellationToken);
		await SetAsync (key, value, expiration);
		return value;
	}

	public Task ClearAsync (CancellationToken cancellationToken = default)
	{
		lock (_keysLock) {
			foreach (var key in _keys) {
				_cache.Remove (key);
			}
			_keys.Clear ();
		}

		_logger.LogInformation ("Cache cleared");
		return Task.CompletedTask;
	}

	public Task RemoveByPatternAsync (string pattern, CancellationToken cancellationToken = default)
	{
		try {
			lock (_keysLock) {
				var keysToRemove = _keys.Where(k => k.Contains(pattern)).ToList();
				foreach (var key in keysToRemove) {
					_cache.Remove(key);
					_keys.Remove(key);
				}
			}
			_logger.LogDebug("Removed keys matching pattern: {Pattern}", pattern);
			return Task.CompletedTask;
		} catch (Exception ex) {
			_logger.LogError(ex, "Error removing keys matching pattern {Pattern} from cache", pattern);
			return Task.CompletedTask;
		}
	}

	public Task<bool> ExistsAsync (string key, CancellationToken cancellationToken = default)
	{
		try {
			var exists = _cache.TryGetValue(key, out _);
			return Task.FromResult(exists);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error checking existence of key {Key} in cache", key);
			return Task.FromResult(false);
		}
	}

	public CacheStatistics GetStatistics ()
	{
		lock (_keysLock) {
			return new CacheStatistics {
				EntryCount = _keys.Count,
				HitRate = 0.0, // Simple implementation - would need tracking for real stats
				TotalSizeBytes = 0, // Simple implementation - would need actual size calculation
				HitCount = 0,
				MissCount = 0
			};
		}
	}
}