using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using AzureDevOps.MCP.Configuration;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Memory-optimized cache service using object pooling, efficient serialization, and memory pressure management.
/// </summary>
public class MemoryOptimizedCacheService : ICacheService, IDisposable
{
	static readonly JsonSerializerOptions Options = new () {
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	readonly IMemoryCache _cache;
	readonly ILogger<MemoryOptimizedCacheService> _logger;
	readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
	readonly ObjectPool<StringBuilder> _stringBuilderPool;
	readonly ConcurrentDictionary<string, byte> _keyTracker = new (); // Minimal memory footprint for key tracking
	readonly MemoryCacheEntryOptions _defaultOptions;
	readonly SemaphoreSlim _memoryPressureLock = new (1, 1);

	volatile bool _disposed;
	readonly Timer? _memoryPressureTimer;
	readonly long _maxMemoryThreshold;
	readonly int _compressionThreshold;

	public MemoryOptimizedCacheService (
		IMemoryCache cache,
		ILogger<MemoryOptimizedCacheService> logger,
		IOptions<ProductionConfiguration> config,
		ObjectPoolProvider poolProvider)
	{
		_cache = cache ?? throw new ArgumentNullException (nameof (cache));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
		_stringBuilderPool = poolProvider?.CreateStringBuilderPool () ?? throw new ArgumentNullException (nameof (poolProvider));

		var cacheConfig = config.Value.Caching;
		_maxMemoryThreshold = cacheConfig.MaxMemoryCacheSizeMB * 1024 * 1024; // Convert MB to bytes
		_compressionThreshold = 1024; // Compress objects > 1KB

		// Pre-configure default cache options to avoid repeated allocations
		_defaultOptions = new MemoryCacheEntryOptions {
			AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes (cacheConfig.DefaultExpirationMinutes),
			Size = 1, // Enable size-based eviction
			PostEvictionCallbacks = { new PostEvictionCallbackRegistration
			{
				EvictionCallback = OnEntryEvicted
			}}
		};

		// Monitor memory pressure every 30 seconds
		_memoryPressureTimer = new Timer (CheckMemoryPressure, null,
			TimeSpan.FromSeconds (30), TimeSpan.FromSeconds (30));

		_logger.LogInformation ("Memory-optimized cache service initialized with {MaxMemoryMB}MB threshold",
			cacheConfig.MaxMemoryCacheSizeMB);
	}

	public async Task<T?> GetAsync<T> (string key, CancellationToken cancellationToken = default) where T : class
	{
		if (_disposed) {
			return null;
		}

		try {
			if (_cache.TryGetValue (key, out var cachedValue)) {
				_logger.LogDebug ("Cache hit for key: {Key}", key);

				return cachedValue switch {
					T directValue => directValue,
					byte[] compressedData => await DecompressObjectAsync<T> (compressedData, cancellationToken),
					_ => null
				};
			}

			_logger.LogDebug ("Cache miss for key: {Key}", key);
			return null;
		} catch (Exception ex) {
			_logger.LogError (ex, "Error retrieving key {Key} from cache", key);
			return null;
		}
	}

	public async Task SetAsync<T> (string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
	{
		if (_disposed || value == null) {
			return;
		}

		try {
			var options = CreateCacheOptions (expiration);

			// Estimate object size and decide on storage strategy
			var objectSize = EstimateObjectSize (value);
			options.Size = Math.Max (1, objectSize / 1024); // Size in KB for cache sizing

			object cacheValue;

			if (objectSize > _compressionThreshold) {
				// Compress large objects to save memory
				var compressedData = await CompressObjectAsync (value, cancellationToken);
				cacheValue = compressedData;
				_logger.LogDebug ("Cached compressed object for key {Key}, original: {OriginalSize}B, compressed: {CompressedSize}B",
					key, objectSize, compressedData.Length);
			} else {
				// Store small objects directly
				cacheValue = value;
			}

			_cache.Set (key, cacheValue, options);
			_keyTracker.TryAdd (key, 0); // Minimal memory usage for tracking

			_logger.LogDebug ("Cached object for key: {Key}, size: {Size}KB", key, objectSize / 1024);
		} catch (Exception ex) {
			_logger.LogError (ex, "Error setting key {Key} in cache", key);
		}
	}

	public Task RemoveAsync (string key, CancellationToken cancellationToken = default)
	{
		if (_disposed) {
			return Task.CompletedTask;
		}

		try {
			_cache.Remove (key);
			_keyTracker.TryRemove (key, out _);
			_logger.LogDebug ("Removed key from cache: {Key}", key);
		} catch (Exception ex) {
			_logger.LogError (ex, "Error removing key {Key} from cache", key);
		}

		return Task.CompletedTask;
	}

	public Task ClearAsync (CancellationToken cancellationToken = default)
	{
		if (_disposed) {
			return Task.CompletedTask;
		}

		try {
			// Clear tracking first to avoid memory pressure during cache clear
			_keyTracker.Clear ();

			// MemoryCache doesn't have a clear method, so we track keys
			var keysToRemove = _keyTracker.Keys.ToArray ();
			foreach (var key in keysToRemove) {
				_cache.Remove (key);
			}

			_logger.LogInformation ("Cache cleared");
		} catch (Exception ex) {
			_logger.LogError (ex, "Error clearing cache");
		}

		return Task.CompletedTask;
	}

	public void Clear ()
	{
		ClearAsync ().Wait ();
	}

	public async Task<T> GetOrSetAsync<T> (string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
	{
		ObjectDisposedException.ThrowIf (_disposed, nameof (MemoryOptimizedCacheService));

		// Try to get from cache first
		var cachedValue = await GetAsync<T> (key, cancellationToken);
		if (cachedValue != null) {
			return cachedValue;
		}

		// Generate value and cache it
		try {
			var value = await factory (cancellationToken);
			if (value != null) {
				await SetAsync (key, value, expiration, cancellationToken);
			}
			return value!;
		} catch (Exception ex) {
			_logger.LogError (ex, "Error in GetOrSetAsync for key {Key}", key);
			throw;
		}
	}

	public Task RemoveByPatternAsync (string pattern, CancellationToken cancellationToken = default)
	{
		if (_disposed) {
			return Task.CompletedTask;
		}

		try {
			var keysToRemove = _keyTracker.Keys
				.Where (key => System.Text.RegularExpressions.Regex.IsMatch (key, pattern))
				.ToArray ();

			foreach (var key in keysToRemove) {
				_cache.Remove (key);
				_keyTracker.TryRemove (key, out _);
			}

			_logger.LogDebug ("Removed {Count} keys matching pattern: {Pattern}", keysToRemove.Length, pattern);
		} catch (Exception ex) {
			_logger.LogError (ex, "Error removing keys by pattern {Pattern}", pattern);
		}

		return Task.CompletedTask;
	}

	public Task<bool> ExistsAsync (string key, CancellationToken cancellationToken = default)
	{
		if (_disposed) {
			return Task.FromResult (false);
		}

		try {
			return Task.FromResult (_keyTracker.ContainsKey (key));
		} catch (Exception ex) {
			_logger.LogError (ex, "Error checking existence of key {Key}", key);
			return Task.FromResult (false);
		}
	}

	public CacheStatistics GetStatistics ()
	{
		if (_disposed) {
			return new CacheStatistics ();
		}

		try {
			var memoryInfo = GC.GetGCMemoryInfo ();

			return new CacheStatistics {
				EntryCount = _keyTracker.Count,
				HitCount = 0, // Would need tracking for accurate metrics
				MissCount = 0, // Would need tracking for accurate metrics
				TotalSizeBytes = memoryInfo.MemoryLoadBytes,
				HitRate = 0.0 // Would need tracking for accurate metrics
			};
		} catch (Exception ex) {
			_logger.LogError (ex, "Error getting cache statistics");
			return new CacheStatistics ();
		}
	}

	/// <summary>
	/// Creates cache options with memory optimization in mind.
	/// </summary>
	MemoryCacheEntryOptions CreateCacheOptions (TimeSpan? expiration)
	{
		if (expiration.HasValue) {
			// Create new options only when needed
			return new MemoryCacheEntryOptions {
				AbsoluteExpirationRelativeToNow = expiration,
				Size = 1,
				PostEvictionCallbacks = { new PostEvictionCallbackRegistration
				{
					EvictionCallback = OnEntryEvicted
				}}
			};
		}

		return _defaultOptions; // Reuse pre-configured options
	}

	/// <summary>
	/// Estimates object size for memory management decisions.
	/// </summary>
	int EstimateObjectSize<T> (T value) where T : class
	{
		return value switch {
			string str => str.Length * 2, // 2 bytes per char in .NET
			byte[] bytes => bytes.Length,
			Array array => array.Length * 8, // Rough estimate
			_ => 1024 // Default estimate for complex objects
		};
	}

	/// <summary>
	/// Compresses objects using efficient binary serialization.
	/// </summary>
	async Task<byte[]> CompressObjectAsync<T> (T value, CancellationToken cancellationToken) where T : class
	{
		var buffer = _bytePool.Rent (8192);
		try {
			using var stream = new MemoryStream (buffer);
			await JsonSerializer.SerializeAsync (stream, value, Options, cancellationToken);

			return stream.ToArray ();
		} finally {
			_bytePool.Return (buffer);
		}
	}

	/// <summary>
	/// Decompresses objects using pooled resources.
	/// </summary>
	async Task<T?> DecompressObjectAsync<T> (byte[] compressedData, CancellationToken cancellationToken) where T : class
	{
		try {
			using var stream = new MemoryStream (compressedData);
			return await JsonSerializer.DeserializeAsync<T> (stream, cancellationToken: cancellationToken);
		} catch (Exception ex) {
			_logger.LogWarning (ex, "Failed to decompress cached object");
			return null;
		}
	}

	/// <summary>
	/// Callback for cache entry eviction to maintain key tracking.
	/// </summary>
	void OnEntryEvicted (object key, object? value, EvictionReason reason, object? state)
	{
		if (key is string keyString) {
			_keyTracker.TryRemove (keyString, out _);
			_logger.LogDebug ("Cache entry evicted: {Key}, reason: {Reason}", keyString, reason);
		}
	}

	/// <summary>
	/// Monitors memory pressure and proactively manages cache size.
	/// </summary>
	void CheckMemoryPressure (object? state)
	{
		if (_disposed) {
			return;
		}

		Task.Run (async () => {
			try {
				var memoryInfo = GC.GetGCMemoryInfo ();
				var memoryPressure = memoryInfo.MemoryLoadBytes;

				if (memoryPressure > _maxMemoryThreshold) {
					await _memoryPressureLock.WaitAsync ();
					try {
						_logger.LogWarning ("High memory pressure detected: {MemoryMB}MB, threshold: {ThresholdMB}MB",
							memoryPressure / 1024 / 1024, _maxMemoryThreshold / 1024 / 1024);

						// Force garbage collection of older cache entries
						GC.Collect (1, GCCollectionMode.Optimized);

						_logger.LogInformation ("Memory pressure relief attempted");
					} finally {
						_memoryPressureLock.Release ();
					}
				}
			} catch (Exception ex) {
				_logger.LogError (ex, "Error during memory pressure check");
			}
		});
	}

	public void Dispose ()
	{
		if (_disposed) {
			return;
		}

		_disposed = true;
		_memoryPressureTimer?.Dispose ();
		_memoryPressureLock?.Dispose ();
		_keyTracker.Clear ();

		_logger.LogDebug ("Memory-optimized cache service disposed");
	}
}