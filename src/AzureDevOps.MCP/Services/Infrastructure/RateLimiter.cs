using System.Collections.Concurrent;
using System.Diagnostics;

namespace AzureDevOps.MCP.Services.Infrastructure;

public class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly RateLimitOptions _options;
    private readonly ILogger<SlidingWindowRateLimiter> _logger;
    private readonly ConcurrentDictionary<string, RequestWindow> _windows = new();
    private readonly Timer _cleanupTimer;

    public SlidingWindowRateLimiter(RateLimitOptions options, ILogger<SlidingWindowRateLimiter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _cleanupTimer = new Timer(CleanupExpiredWindows, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<bool> TryAcquireAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

        var window = _windows.GetOrAdd(identifier, _ => new RequestWindow(_options.WindowSize));
        var now = DateTime.UtcNow;

        lock (window)
        {
            // Remove old requests outside the window
            window.Requests.RemoveAll(time => now - time > _options.WindowSize);

            if (window.Requests.Count >= _options.RequestsPerWindow)
            {
                _logger.LogWarning("Rate limit exceeded for identifier: {Identifier} ({Count}/{Limit})", 
                    identifier, window.Requests.Count, _options.RequestsPerWindow);

                if (_options.ThrowOnLimit)
                {
                    var status = GetStatusInternal(identifier, window, now);
                    throw new RateLimitExceededException(status);
                }

                return false;
            }

            window.Requests.Add(now);
            _logger.LogTrace("Request allowed for identifier: {Identifier} ({Count}/{Limit})", 
                identifier, window.Requests.Count, _options.RequestsPerWindow);
            
            return true;
        }
    }

    public async Task<RateLimitStatus> GetStatusAsync(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

        var window = _windows.GetOrAdd(identifier, _ => new RequestWindow(_options.WindowSize));
        var now = DateTime.UtcNow;

        lock (window)
        {
            return GetStatusInternal(identifier, window, now);
        }
    }

    public void Reset(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return;

        if (_windows.TryGetValue(identifier, out var window))
        {
            lock (window)
            {
                window.Requests.Clear();
            }
        }

        _logger.LogDebug("Rate limit reset for identifier: {Identifier}", identifier);
    }

    public void ResetAll()
    {
        foreach (var kvp in _windows)
        {
            lock (kvp.Value)
            {
                kvp.Value.Requests.Clear();
            }
        }

        _logger.LogInformation("All rate limits reset");
    }

    private RateLimitStatus GetStatusInternal(string identifier, RequestWindow window, DateTime now)
    {
        // Remove old requests
        window.Requests.RemoveAll(time => now - time > _options.WindowSize);

        var requestsInWindow = window.Requests.Count;
        var requestsRemaining = Math.Max(0, _options.RequestsPerWindow - requestsInWindow);
        
        var oldestRequest = window.Requests.Count > 0 ? window.Requests.Min() : now;
        var windowStart = now - _options.WindowSize;
        var nextReset = requestsRemaining == 0 && window.Requests.Count > 0 
            ? window.Requests.Min() + _options.WindowSize 
            : (DateTime?)null;

        return new RateLimitStatus
        {
            RequestsRemaining = requestsRemaining,
            RequestsPerWindow = _options.RequestsPerWindow,
            WindowSize = _options.WindowSize,
            WindowStart = windowStart,
            NextReset = nextReset
        };
    }

    private void CleanupExpiredWindows(object? state)
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<string>();

        foreach (var kvp in _windows)
        {
            lock (kvp.Value)
            {
                kvp.Value.Requests.RemoveAll(time => now - time > _options.WindowSize);
                
                // Remove windows that haven't been used recently
                if (kvp.Value.Requests.Count == 0 && now - kvp.Value.LastAccess > TimeSpan.FromMinutes(10))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _windows.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limit windows", keysToRemove.Count);
        }
    }

    private class RequestWindow
    {
        public List<DateTime> Requests { get; } = new();
        public DateTime LastAccess { get; set; }
        
        public RequestWindow(TimeSpan windowSize)
        {
            LastAccess = DateTime.UtcNow;
        }
    }
}

public class HealthCheckService : IHealthCheckService, IDisposable
{
    private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<HealthStatus>>> _checks = new();
    private readonly ConcurrentDictionary<string, HealthStatus> _lastResults = new();
    private readonly ILogger<HealthCheckService> _logger;
    private readonly Timer _periodicCheckTimer;
    private bool _disposed;

    public event EventHandler<HealthChangedEventArgs>? HealthChanged;

    public HealthCheckService(ILogger<HealthCheckService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _periodicCheckTimer = new Timer(PeriodicHealthCheck, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var allResults = await CheckAllHealthAsync(cancellationToken);
        
        var isHealthy = allResults.Values.All(status => status.IsHealthy);
        var unhealthyChecks = allResults.Where(kvp => !kvp.Value.IsHealthy).ToList();

        if (isHealthy)
        {
            return HealthStatus.Healthy("All health checks passed", new Dictionary<string, object>
            {
                ["totalChecks"] = allResults.Count,
                ["details"] = allResults
            });
        }

        var description = $"{unhealthyChecks.Count} of {allResults.Count} health checks failed";
        return HealthStatus.Unhealthy(description, data: new Dictionary<string, object>
        {
            ["totalChecks"] = allResults.Count,
            ["failedChecks"] = unhealthyChecks.Count,
            ["details"] = allResults
        });
    }

    public async Task<Dictionary<string, HealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, HealthStatus>();
        var tasks = _checks.Select(async kvp =>
        {
            var (name, check) = kvp;
            var sw = Stopwatch.StartNew();
            
            try
            {
                var result = await check(cancellationToken);
                sw.Stop();
                
                var statusWithTiming = new HealthStatus
                {
                    IsHealthy = result.IsHealthy,
                    Description = result.Description,
                    Data = result.Data,
                    Exception = result.Exception,
                    ResponseTime = sw.Elapsed
                };

                return (name, status: statusWithTiming);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Health check failed for {CheckName}", name);
                
                return (name, status: HealthStatus.Unhealthy(
                    $"Health check threw exception: {ex.Message}", 
                    ex, 
                    responseTime: sw.Elapsed));
            }
        });

        var completedTasks = await Task.WhenAll(tasks);
        
        foreach (var (name, status) in completedTasks)
        {
            results[name] = status;
            
            // Check if status changed
            if (_lastResults.TryGetValue(name, out var previousStatus) && 
                previousStatus.IsHealthy != status.IsHealthy)
            {
                HealthChanged?.Invoke(this, new HealthChangedEventArgs(name, status, previousStatus));
            }
            
            _lastResults[name] = status;
        }

        return results;
    }

    public void RegisterCheck(string name, Func<CancellationToken, Task<HealthStatus>> check)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Check name cannot be null or empty", nameof(name));
        
        if (check == null)
            throw new ArgumentNullException(nameof(check));

        _checks.AddOrUpdate(name, check, (_, _) => check);
        _logger.LogDebug("Registered health check: {CheckName}", name);
    }

    private async void PeriodicHealthCheck(object? state)
    {
        if (_disposed) return;

        try
        {
            await CheckAllHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic health check");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _periodicCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}