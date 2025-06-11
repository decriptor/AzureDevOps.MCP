using System.Collections.Concurrent;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// In-memory rate limit store using modern .NET 9 concurrent collections.
/// </summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ILogger<InMemoryRateLimitStore> _logger;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _store = new();
    private readonly Timer _cleanupTimer;

    public InMemoryRateLimitStore(ILogger<InMemoryRateLimitStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Cleanup expired entries every 5 minutes using modern timer patterns
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public Task<RateLimitStoreResult> CheckAndIncrementAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var resetTime = now.Add(window);

        var result = _store.AddOrUpdate(key,
            // Add new entry
            new RateLimitEntry(1, resetTime),
            // Update existing entry using modern pattern matching
            (_, existing) => existing.ResetTime <= now
                ? new RateLimitEntry(1, resetTime) // Reset if expired
                : existing with { RequestCount = existing.RequestCount + 1 } // Increment if still valid
        );

        var isAllowed = result.RequestCount <= limit;
        var storeResult = new RateLimitStoreResult(result.RequestCount, isAllowed, result.ResetTime);

        return Task.FromResult(storeResult);
    }

    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<long> GetCurrentCountAsync(string key, CancellationToken cancellationToken = default)
    {
        var count = _store.TryGetValue(key, out var entry) && entry.ResetTime > DateTimeOffset.UtcNow 
            ? entry.RequestCount 
            : 0;
        
        return Task.FromResult((long)count);
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _store
                .Where(kvp => kvp.Value.ResetTime <= now)
                .Select(kvp => kvp.Key)
                .ToArray();

            foreach (var key in expiredKeys)
            {
                _store.TryRemove(key, out _);
            }

            if (expiredKeys.Length > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired rate limit entries", expiredKeys.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rate limit cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}