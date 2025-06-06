namespace AzureDevOps.MCP.Services.Infrastructure;

public interface ICacheStrategy
{
    TimeSpan GetExpiration(string cacheKey, object? value = null);
    string GenerateKey(params object[] keyParts);
    bool ShouldCache(object? value);
}

public interface IMemoryPressureManager
{
    bool IsMemoryPressureHigh { get; }
    void CheckMemoryPressure();
    event EventHandler<MemoryPressureEventArgs>? MemoryPressureChanged;
}

public class MemoryPressureEventArgs : EventArgs
{
    public bool IsHighPressure { get; }
    public long MemoryUsageBytes { get; }
    
    public MemoryPressureEventArgs(bool isHighPressure, long memoryUsageBytes)
    {
        IsHighPressure = isHighPressure;
        MemoryUsageBytes = memoryUsageBytes;
    }
}

public class DefaultCacheStrategy : ICacheStrategy
{
    public TimeSpan GetExpiration(string cacheKey, object? value = null)
    {
        return cacheKey switch
        {
            var key when key.StartsWith("projects") => Constants.Cache.ProjectsCacheExpiration,
            var key when key.StartsWith("repos_") => Constants.Cache.RepositoriesCacheExpiration,
            var key when key.StartsWith("items_") => Constants.Cache.FileContentCacheExpiration,
            var key when key.StartsWith("file_") => Constants.Cache.FileContentCacheExpiration,
            var key when key.StartsWith("workitems_") => Constants.Cache.WorkItemsCacheExpiration,
            var key when key.StartsWith("workitem_") => Constants.Cache.WorkItemsCacheExpiration,
            var key when key.StartsWith("builds_") => Constants.Cache.BuildsCacheExpiration,
            var key when key.StartsWith("testruns_") => Constants.Cache.TestRunsCacheExpiration,
            var key when key.StartsWith("wikis_") => Constants.Cache.WikiCacheExpiration,
            var key when key.StartsWith("wikipage_") => Constants.Cache.WikiCacheExpiration,
            _ => TimeSpan.FromMinutes(2) // Default fallback
        };
    }

    public string GenerateKey(params object[] keyParts)
    {
        if (keyParts == null || keyParts.Length == 0)
            throw new ArgumentException("Key parts cannot be null or empty", nameof(keyParts));

        return string.Join("_", keyParts.Select(part => 
            part?.ToString()?.Replace('/', '_').Replace('\\', '_').Replace(':', '_') ?? "null"));
    }

    public bool ShouldCache(object? value)
    {
        return value switch
        {
            null => false,
            string str when string.IsNullOrEmpty(str) => false,
            System.Collections.IEnumerable enumerable when !enumerable.Cast<object>().Any() => false,
            _ => true
        };
    }
}

public class MemoryPressureManager : IMemoryPressureManager, IDisposable
{
    private readonly Timer _memoryCheckTimer;
    private readonly long _highPressureThresholdBytes;
    private readonly ILogger<MemoryPressureManager> _logger;
    private bool _isHighPressure;
    private bool _disposed;

    public event EventHandler<MemoryPressureEventArgs>? MemoryPressureChanged;

    public bool IsMemoryPressureHigh => _isHighPressure;

    public MemoryPressureManager(ILogger<MemoryPressureManager> logger, long highPressureThresholdBytes = 1_000_000_000) // 1GB default
    {
        _logger = logger;
        _highPressureThresholdBytes = highPressureThresholdBytes;
        _memoryCheckTimer = new Timer(CheckMemoryPressureCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public void CheckMemoryPressure()
    {
        if (_disposed) return;

        try
        {
            var memoryUsage = GC.GetTotalMemory(false);
            var wasHighPressure = _isHighPressure;
            _isHighPressure = memoryUsage > _highPressureThresholdBytes;

            if (wasHighPressure != _isHighPressure)
            {
                _logger.LogInformation("Memory pressure changed: {IsHigh} (Usage: {Usage:N0} bytes)", 
                    _isHighPressure ? "HIGH" : "NORMAL", memoryUsage);
                
                MemoryPressureChanged?.Invoke(this, new MemoryPressureEventArgs(_isHighPressure, memoryUsage));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking memory pressure");
        }
    }

    private void CheckMemoryPressureCallback(object? state)
    {
        CheckMemoryPressure();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}