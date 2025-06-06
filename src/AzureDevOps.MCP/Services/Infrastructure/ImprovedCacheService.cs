using AzureDevOps.MCP.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace AzureDevOps.MCP.Services.Infrastructure;

public class ImprovedCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ICacheStrategy _strategy;
    private readonly IMemoryPressureManager _memoryPressureManager;
    private readonly ILogger<ImprovedCacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _keyAccessTimes = new();
    private readonly Timer _cleanupTimer;
    private readonly object _cleanupLock = new();
    private bool _disposed;

    public ImprovedCacheService(
        IMemoryCache cache,
        ICacheStrategy strategy,
        IMemoryPressureManager memoryPressureManager,
        ILogger<ImprovedCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _memoryPressureManager = memoryPressureManager ?? throw new ArgumentNullException(nameof(memoryPressureManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _memoryPressureManager.MemoryPressureChanged += OnMemoryPressureChanged;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _keyAccessTimes.TryAdd(key, DateTime.UtcNow);
                _logger.LogTrace("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogTrace("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key {Key} from cache", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value == null)
        {
            _logger.LogTrace("Skipping cache set for null value with key: {Key}", key);
            return;
        }

        if (!_strategy.ShouldCache(value))
        {
            _logger.LogTrace("Strategy determined not to cache value for key: {Key}", key);
            return;
        }

        try
        {
            var effectiveExpiration = expiration ?? _strategy.GetExpiration(key, value);
            
            // Check if we're near memory limits
            if (_memoryPressureManager.IsMemoryPressureHigh)
            {
                effectiveExpiration = TimeSpan.FromMilliseconds(effectiveExpiration.TotalMilliseconds * 0.5); // Reduce expiration
                _logger.LogDebug("Reduced cache expiration due to memory pressure for key: {Key}", key);
            }

            // Check cache size limits
            if (_keyAccessTimes.Count >= Constants.Cache.MaxCacheKeysToTrack)
            {
                await EvictLeastRecentlyUsedAsync();
            }

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveExpiration,
                Priority = CacheItemPriority.Normal,
                Size = EstimateSize(value)
            };

            options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                _keyAccessTimes.TryRemove(evictedKey.ToString()!, out _);
                _logger.LogTrace("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
            });

            _cache.Set(key, value, options);
            _keyAccessTimes.TryAdd(key, DateTime.UtcNow);

            _logger.LogTrace("Cached key: {Key} with expiration: {Expiration}", key, effectiveExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in cache", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(key))
            return;

        try
        {
            _cache.Remove(key);
            _keyAccessTimes.TryRemove(key, out _);
            _logger.LogTrace("Removed key from cache: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from cache", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        ThrowIfDisposed();
        
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in factory method for cache key: {Key}", key);
            throw;
        }
    }

    public void Clear()
    {
        ThrowIfDisposed();
        
        try
        {
            // MemoryCache doesn't have a direct Clear method, so we track keys
            var keysToRemove = _keyAccessTimes.Keys.ToList();
            
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            
            _keyAccessTimes.Clear();
            _logger.LogInformation("Cache cleared ({KeyCount} keys removed)", keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    private async Task EvictLeastRecentlyUsedAsync()
    {
        if (_disposed) return;

        lock (_cleanupLock)
        {
            try
            {
                var sortedKeys = _keyAccessTimes
                    .OrderBy(kvp => kvp.Value)
                    .Take(_keyAccessTimes.Count / 4) // Remove 25% of oldest entries
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in sortedKeys)
                {
                    _cache.Remove(key);
                    _keyAccessTimes.TryRemove(key, out _);
                }

                _logger.LogDebug("Evicted {Count} least recently used cache entries", sortedKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LRU eviction");
            }
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        if (_disposed) return;

        lock (_cleanupLock)
        {
            try
            {
                var expiredKeys = _keyAccessTimes
                    .Where(kvp => DateTime.UtcNow - kvp.Value > TimeSpan.FromHours(1))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _keyAccessTimes.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache key references", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }
    }

    private void OnMemoryPressureChanged(object? sender, MemoryPressureEventArgs e)
    {
        if (e.IsHighPressure)
        {
            _logger.LogWarning("High memory pressure detected, reducing cache size");
            _ = Task.Run(EvictLeastRecentlyUsedAsync);
        }
    }

    private static long EstimateSize(object value)
    {
        return value switch
        {
            string str => str.Length * 2, // Unicode characters
            System.Collections.IEnumerable enumerable => enumerable.Cast<object>().Count() * 100, // Rough estimate
            _ => 100 // Default estimate
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImprovedCacheService));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _keyAccessTimes?.Clear();
            
            if (_memoryPressureManager != null)
            {
                _memoryPressureManager.MemoryPressureChanged -= OnMemoryPressureChanged;
            }
            
            _disposed = true;
            _logger.LogDebug("ImprovedCacheService disposed");
        }
    }
}