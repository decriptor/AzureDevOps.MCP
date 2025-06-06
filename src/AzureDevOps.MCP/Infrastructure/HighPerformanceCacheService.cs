using System.Collections.Frozen;
using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AzureDevOps.MCP.Infrastructure;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    CacheStatistics GetStatistics();
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public class HighPerformanceCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<HighPerformanceCacheService> _logger;
    private readonly CacheConfiguration _configuration;
    
    // .NET 9: Lock-free concurrent collections for high performance
    private readonly ConcurrentDictionary<string, CacheMetadata> _metadata = new();
    private readonly Timer _cleanupTimer;
    private readonly Timer _memoryPressureTimer;
    
    // .NET 9: Use SearchValues for ultra-fast pattern matching
    private static readonly SearchValues<char> InvalidKeyChars = SearchValues.Create(['<', '>', ':', '"', '|', '?', '*', '\0']);
    
    // Cache statistics with high-performance counters
    private long _cacheHits;
    private long _cacheMisses;
    private long _cacheEvictions;
    private long _totalRequests;
    
    // Memory pressure management
    private readonly PeriodicTimer _memoryCheckTimer;
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _disposed;

    // .NET 9: Frozen collections for configuration
    private static readonly FrozenDictionary<string, TimeSpan> DefaultExpirations = new Dictionary<string, TimeSpan>
    {
        ["projects"] = TimeSpan.FromMinutes(10),
        ["repositories"] = TimeSpan.FromMinutes(5),
        ["workitems"] = TimeSpan.FromMinutes(2),
        ["builds"] = TimeSpan.FromMinutes(1),
        ["files"] = TimeSpan.FromMinutes(15),
        ["commits"] = TimeSpan.FromMinutes(30),
        ["branches"] = TimeSpan.FromMinutes(5),
        ["pullrequests"] = TimeSpan.FromMinutes(1)
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public HighPerformanceCacheService(
        IMemoryCache cache,
        ILogger<HighPerformanceCacheService> logger,
        CacheConfiguration configuration)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // .NET 9: Use PeriodicTimer for better resource management
        _memoryCheckTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        
        // Legacy timer for compatibility
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        _memoryPressureTimer = new Timer(CheckMemoryPressure, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        // Start memory monitoring task
        _ = Task.Run(MonitorMemoryPressureAsync, _disposeCts.Token);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        ValidateKey(key);
        
        Interlocked.Increment(ref _totalRequests);

        using var activity = ActivitySource.StartActivity("Cache.Get");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.type", typeof(T).Name);

        try
        {
            if (_cache.TryGetValue(key, out var cached) && cached is T typedValue)
            {
                Interlocked.Increment(ref _cacheHits);
                UpdateAccessTime(key);
                
                activity?.SetTag("cache.hit", true);
                _logger.LogTrace("Cache hit for key: {Key}", key);
                
                return typedValue;
            }

            Interlocked.Increment(ref _cacheMisses);
            activity?.SetTag("cache.hit", false);
            _logger.LogTrace("Cache miss for key: {Key}", key);
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving cache entry for key: {Key}", key);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        using var activity = ActivitySource.StartActivity("Cache.Set");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.type", typeof(T).Name);

        try
        {
            var effectiveExpiration = expiration ?? GetDefaultExpiration(key);
            
            // .NET 9: Use MemoryPressure for intelligent cache sizing
            if (IsMemoryPressureHigh())
            {
                effectiveExpiration = TimeSpan.FromMilliseconds(effectiveExpiration.TotalMilliseconds * 0.5);
                activity?.SetTag("cache.memory_pressure", true);
            }

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveExpiration,
                Priority = GetCachePriority(key),
                Size = EstimateSize(value)
            };

            // Add eviction callback for statistics
            options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                if (reason != EvictionReason.Replaced)
                {
                    Interlocked.Increment(ref _cacheEvictions);
                    _metadata.TryRemove(evictedKey.ToString()!, out _);
                }
            });

            _cache.Set(key, value, options);
            
            // Track metadata
            _metadata[key] = new CacheMetadata
            {
                Key = key,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(effectiveExpiration),
                Size = options.Size ?? 1,
                Type = typeof(T).Name
            };

            activity?.SetTag("cache.expiration_ms", effectiveExpiration.TotalMilliseconds);
            _logger.LogTrace("Cache entry set for key: {Key}, expires in: {ExpirationMs}ms", key, effectiveExpiration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        using var activity = ActivitySource.StartActivity("Cache.GetOrSet");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.factory_execution", true);

        try
        {
            var value = await factory(cancellationToken);
            await SetAsync(key, value, expiration, cancellationToken);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSet factory for key: {Key}", ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateKey(key);

        _cache.Remove(key);
        _metadata.TryRemove(key, out _);
        
        _logger.LogTrace("Cache entry removed for key: {Key}", key);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        using var activity = ActivitySource.StartActivity("Cache.RemoveByPattern");
        activity?.SetTag("cache.pattern", pattern);

        var keysToRemove = new List<string>();
        
        // .NET 9: Use Span for efficient string operations
        ReadOnlySpan<char> patternSpan = pattern.AsSpan();
        
        foreach (var metadata in _metadata.Values)
        {
            if (IsPatternMatch(metadata.Key.AsSpan(), patternSpan))
            {
                keysToRemove.Add(metadata.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            await RemoveAsync(key, cancellationToken);
        }

        activity?.SetTag("cache.removed_count", keysToRemove.Count);
        _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_cache is MemoryCache memoryCache)
        {
            // .NET 9: Use reflection to access private compact method for better performance
            var field = typeof(MemoryCache).GetField("_coherentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(memoryCache) != null)
            {
                memoryCache.Compact(1.0); // Remove all entries
            }
        }

        _metadata.Clear();
        
        // Reset statistics
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
        Interlocked.Exchange(ref _cacheEvictions, 0);
        Interlocked.Exchange(ref _totalRequests, 0);

        _logger.LogInformation("Cache cleared completely");
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateKey(key);
        
        return Task.FromResult(_metadata.ContainsKey(key));
    }

    public CacheStatistics GetStatistics()
    {
        var totalRequests = Interlocked.Read(ref _totalRequests);
        var hitRate = totalRequests > 0 ? (double)Interlocked.Read(ref _cacheHits) / totalRequests : 0.0;

        return new CacheStatistics
        {
            TotalRequests = totalRequests,
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            CacheEvictions = Interlocked.Read(ref _cacheEvictions),
            HitRate = hitRate,
            EntryCount = _metadata.Count,
            TotalSizeBytes = _metadata.Values.Sum(m => m.Size),
            MemoryPressure = IsMemoryPressureHigh()
        };
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        // .NET 9: Ultra-fast validation using SearchValues
        if (key.AsSpan().ContainsAny(InvalidKeyChars))
        {
            throw new ArgumentException("Cache key contains invalid characters", nameof(key));
        }

        if (key.Length > 250)
        {
            throw new ArgumentException("Cache key too long", nameof(key));
        }
    }

    private TimeSpan GetDefaultExpiration(string key)
    {
        // .NET 9: Use ReadOnlySpan for efficient prefix matching
        ReadOnlySpan<char> keySpan = key.AsSpan();
        
        foreach (var kvp in DefaultExpirations)
        {
            if (keySpan.StartsWith(kvp.Key.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return _configuration.DefaultExpiration;
    }

    private static CacheItemPriority GetCachePriority(string key)
    {
        return key.ToLowerInvariant() switch
        {
            var k when k.StartsWith("projects") => CacheItemPriority.High,
            var k when k.StartsWith("user") => CacheItemPriority.High,
            var k when k.StartsWith("builds") => CacheItemPriority.Low,
            var k when k.StartsWith("temp") => CacheItemPriority.NeverRemove,
            _ => CacheItemPriority.Normal
        };
    }

    private static long EstimateSize<T>(T value) where T : class
    {
        return value switch
        {
            string str => str.Length * 2, // Unicode chars are 2 bytes
            byte[] bytes => bytes.Length,
            _ => JsonSerializer.SerializeToUtf8Bytes(value).Length
        };
    }

    private void UpdateAccessTime(string key)
    {
        if (_metadata.TryGetValue(key, out var metadata))
        {
            metadata.LastAccessedAt = DateTime.UtcNow;
        }
    }

    private bool IsMemoryPressureHigh()
    {
        // .NET 9: Use GC.GetTotalAllocatedBytes for more accurate memory tracking
        var allocatedBytes = GC.GetTotalAllocatedBytes(false);
        var pressure = GC.GetTotalMemory(false);
        
        return pressure > _configuration.MemoryPressureThresholdMB * 1024 * 1024 ||
               allocatedBytes > pressure * 0.8; // 80% of current memory
    }

    private static bool IsPatternMatch(ReadOnlySpan<char> key, ReadOnlySpan<char> pattern)
    {
        // Simple wildcard matching - can be enhanced with more sophisticated algorithms
        if (pattern.Contains('*'))
        {
            var parts = pattern.ToString().Split('*', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return true;
            
            var keyStr = key.ToString();
            var lastIndex = 0;
            
            foreach (var part in parts)
            {
                var index = keyStr.IndexOf(part, lastIndex, StringComparison.OrdinalIgnoreCase);
                if (index == -1) return false;
                lastIndex = index + part.Length;
            }
            return true;
        }
        
        return key.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private async Task MonitorMemoryPressureAsync()
    {
        try
        {
            while (await _memoryCheckTimer.WaitForNextTickAsync(_disposeCts.Token))
            {
                if (IsMemoryPressureHigh())
                {
                    await CleanupLeastRecentlyUsedAsync();
                }
            }
        }
        catch (OperationCanceledException) when (_disposeCts.Token.IsCancellationRequested)
        {
            // Expected during disposal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in memory pressure monitoring");
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _metadata.Values
                .Where(m => m.ExpiresAt < now)
                .Select(m => m.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
                _metadata.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    private void CheckMemoryPressure(object? state)
    {
        if (IsMemoryPressureHigh())
        {
            _ = Task.Run(CleanupLeastRecentlyUsedAsync);
        }
    }

    private async Task CleanupLeastRecentlyUsedAsync()
    {
        var targetRemovalCount = Math.Max(1, _metadata.Count / 10); // Remove 10% of entries
        
        var lruEntries = _metadata.Values
            .OrderBy(m => m.LastAccessedAt)
            .Take(targetRemovalCount)
            .Select(m => m.Key)
            .ToList();

        foreach (var key in lruEntries)
        {
            _cache.Remove(key);
            _metadata.TryRemove(key, out _);
        }

        Interlocked.Add(ref _cacheEvictions, lruEntries.Count);
        _logger.LogInformation("Memory pressure cleanup: removed {Count} least recently used entries", lruEntries.Count);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HighPerformanceCacheService));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _disposeCts.Cancel();
            _cleanupTimer?.Dispose();
            _memoryPressureTimer?.Dispose();
            _memoryCheckTimer?.Dispose();
            _disposeCts.Dispose();
        }
    }

    private static readonly ActivitySource ActivitySource = new("AzureDevOps.MCP.Cache");
}

public class CacheMetadata
{
    public required string Key { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastAccessedAt { get; set; }
    public DateTime ExpiresAt { get; init; }
    public long Size { get; init; }
    public required string Type { get; init; }
}

public class CacheStatistics
{
    public long TotalRequests { get; init; }
    public long CacheHits { get; init; }
    public long CacheMisses { get; init; }
    public long CacheEvictions { get; init; }
    public double HitRate { get; init; }
    public int EntryCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public bool MemoryPressure { get; init; }
}

public class CacheConfiguration
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public long MemoryPressureThresholdMB { get; set; } = 1000;
    public int MaxEntries { get; set; } = 10000;
    public bool EnableMemoryPressureManagement { get; set; } = true;
}