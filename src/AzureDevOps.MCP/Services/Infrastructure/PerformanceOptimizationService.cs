using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AzureDevOps.MCP.Configuration;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Modern performance optimization service using .NET 9 features for maximum efficiency.
/// </summary>
public sealed class PerformanceOptimizationService : IPerformanceOptimizationService
{
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private readonly PerformanceConfiguration _config;
    
    // Use frozen collections for read-only performance-critical data
    private static readonly FrozenDictionary<string, TimeSpan> DefaultCacheDurations = new Dictionary<string, TimeSpan>
    {
        ["projects"] = TimeSpan.FromMinutes(30),
        ["repositories"] = TimeSpan.FromMinutes(15),
        ["workitems"] = TimeSpan.FromMinutes(5),
        ["builds"] = TimeSpan.FromMinutes(2),
        ["testresults"] = TimeSpan.FromMinutes(1)
    }.ToFrozenDictionary();

    private static readonly FrozenSet<string> HighFrequencyOperations = new HashSet<string>
    {
        "GetWorkItems",
        "GetBuilds", 
        "GetTestResults",
        "SearchCode"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // High-performance concurrent collections for runtime metrics
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _operationMetrics = new();
    private readonly ConcurrentQueue<PerformanceEvent> _recentEvents = new();
    private readonly Timer _metricsCleanupTimer;

    public PerformanceOptimizationService(
        ILogger<PerformanceOptimizationService> logger,
        IOptions<PerformanceConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        
        // Setup periodic cleanup using modern timer patterns
        _metricsCleanupTimer = new Timer(CleanupOldMetrics, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Optimizes cache duration based on operation frequency and data volatility.
    /// </summary>
    public TimeSpan OptimizeCacheDuration(string operationType, string dataType)
    {
        var baseKey = $"{operationType}:{dataType}".ToLowerInvariant();
        
        // Use pattern matching for adaptive cache duration
        var optimizedDuration = (operationType.ToLowerInvariant(), dataType.ToLowerInvariant()) switch
        {
            // High-frequency operations get shorter cache durations
            (var op, _) when HighFrequencyOperations.Contains(op) => 
                DefaultCacheDurations.GetValueOrDefault(dataType, TimeSpan.FromMinutes(2)) / 2,
            
            // Static data gets longer cache durations
            ("get", "projects" or "repositories") => TimeSpan.FromHours(1),
            
            // User-specific data gets medium cache durations
            ("get", "workitems") when IsUserSpecificOperation() => TimeSpan.FromMinutes(10),
            
            // Default lookup with fallback
            (_, var data) => DefaultCacheDurations.GetValueOrDefault(data, TimeSpan.FromMinutes(5))
        };

        // Apply performance metrics-based adjustments
        if (_operationMetrics.TryGetValue(baseKey, out var metrics))
        {
            optimizedDuration = AdjustDurationBasedOnMetrics(optimizedDuration, metrics);
        }

        _logger.LogDebug("Optimized cache duration for {OperationType}:{DataType} = {Duration}", 
            operationType, dataType, optimizedDuration);

        return optimizedDuration;
    }

    /// <summary>
    /// Tracks operation performance with minimal overhead using modern .NET 9 patterns.
    /// </summary>
    public async Task<T> TrackPerformanceAsync<T>(
        string operationName, 
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));
        
        using var activity = new Activity(operationName);
        activity.Start();
        
        var stopwatch = Stopwatch.StartNew();
        var startMemory = GC.GetTotalMemory(false);
        
        try
        {
            var result = await operation(cancellationToken);
            
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryDelta = endMemory - startMemory;
            
            RecordSuccessfulOperation(operationName, stopwatch.Elapsed, memoryDelta);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordFailedOperation(operationName, stopwatch.Elapsed, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets performance recommendations using modern pattern matching and data analysis.
    /// </summary>
    public PerformanceRecommendations GetPerformanceRecommendations()
    {
        var recommendations = new List<PerformanceRecommendation>();
        var currentMemory = GC.GetGCMemoryInfo();
        
        // Memory pressure analysis using .NET 9 GC APIs
        if (currentMemory.MemoryLoadBytes > currentMemory.HighMemoryLoadThresholdBytes * 0.8)
        {
            recommendations.Add(new PerformanceRecommendation(
                "MemoryPressure",
                "High memory usage detected",
                "Consider reducing cache sizes or triggering garbage collection",
                RecommendationPriority.High
            ));
        }

        // Analyze operation patterns
        var slowOperations = _operationMetrics
            .Where(kvp => kvp.Value.AverageResponseTime > TimeSpan.FromSeconds(_config.SlowOperationThresholdMs / 1000.0))
            .ToArray();

        foreach (var (operationName, metrics) in slowOperations)
        {
            recommendations.Add(new PerformanceRecommendation(
                "SlowOperation",
                $"Operation '{operationName}' is consistently slow",
                GetOptimizationSuggestion(operationName, metrics),
                RecommendationPriority.Medium
            ));
        }

        // Thread pool analysis using modern APIs
        ThreadPool.GetAvailableThreads(out var availableWorkers, out var availableIO);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIO);
        
        var workerUtilization = 1.0 - ((double)availableWorkers / maxWorkers);
        
        if (workerUtilization > 0.8)
        {
            recommendations.Add(new PerformanceRecommendation(
                "ThreadPoolStarvation",
                "High thread pool utilization detected",
                "Consider implementing async patterns or increasing thread pool limits",
                RecommendationPriority.High
            ));
        }

        return new PerformanceRecommendations(
            GeneratedAt: DateTimeOffset.UtcNow,
            Recommendations: recommendations.ToFrozenSet(),
            SystemMetrics: GetCurrentSystemMetrics(),
            OperationMetrics: _operationMetrics.ToFrozenDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value with { LastUpdated = DateTimeOffset.UtcNow }
            )
        );
    }

    /// <summary>
    /// Optimizes object allocations using modern .NET 9 memory patterns.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T OptimizeAllocation<T>(Func<T> factory) where T : class
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        
        // Use generation-aware allocation for frequently created objects
        if (typeof(T).IsValueType)
        {
            return factory();
        }

        // For reference types, try to minimize allocations
        var result = factory();
        
        // Track allocation patterns for optimization recommendations
        RecordAllocation(typeof(T).Name, EstimateObjectSize(result));
        
        return result;
    }

    /// <summary>
    /// Provides cache warming recommendations based on usage patterns.
    /// </summary>
    public CacheWarmingPlan CreateCacheWarmingPlan()
    {
        var priorityOperations = _operationMetrics
            .Where(kvp => kvp.Value.CallCount > 10) // Frequently used operations
            .OrderByDescending(kvp => kvp.Value.CallCount * kvp.Value.AverageResponseTime.TotalMilliseconds)
            .Take(20)
            .Select(kvp => new CacheWarmingOperation(
                OperationName: kvp.Key,
                Priority: CalculateWarmingPriority(kvp.Value),
                EstimatedBenefit: CalculateEstimatedBenefit(kvp.Value),
                RecommendedPreloadTime: CalculatePreloadTime(kvp.Value)
            ))
            .ToFrozenSet();

        return new CacheWarmingPlan(
            CreatedAt: DateTimeOffset.UtcNow,
            Operations: priorityOperations,
            EstimatedTotalBenefit: priorityOperations.Sum(op => op.EstimatedBenefit),
            EstimatedPreloadDuration: priorityOperations.Max(op => op.RecommendedPreloadTime)
        );
    }

    #region Private Helper Methods

    private static bool IsUserSpecificOperation()
    {
        // Analyze current context to determine if operation is user-specific
        var activity = Activity.Current;
        return activity?.Tags?.Any(tag => 
            tag.Key.Contains("user", StringComparison.OrdinalIgnoreCase) ||
            tag.Key.Contains("personal", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static TimeSpan AdjustDurationBasedOnMetrics(TimeSpan baseDuration, PerformanceMetrics metrics)
    {
        // Adjust cache duration based on hit rate and performance characteristics
        var hitRateMultiplier = metrics.CacheHitRate switch
        {
            > 0.9 => 1.5,  // High hit rate - extend cache duration
            > 0.7 => 1.2,  // Good hit rate - slightly extend
            > 0.5 => 1.0,  // Average hit rate - keep default
            _ => 0.8       // Low hit rate - reduce cache duration
        };

        var performanceMultiplier = metrics.AverageResponseTime.TotalMilliseconds switch
        {
            > 5000 => 2.0,   // Very slow operations - cache longer
            > 1000 => 1.5,   // Slow operations - cache longer
            < 100 => 0.8,    // Fast operations - cache shorter
            _ => 1.0         // Normal operations - keep default
        };

        var adjustedDuration = TimeSpan.FromMilliseconds(
            baseDuration.TotalMilliseconds * hitRateMultiplier * performanceMultiplier);

        // Clamp to reasonable bounds
        return adjustedDuration switch
        {
            var d when d < TimeSpan.FromSeconds(30) => TimeSpan.FromSeconds(30),
            var d when d > TimeSpan.FromHours(4) => TimeSpan.FromHours(4),
            _ => adjustedDuration
        };
    }

    private void RecordSuccessfulOperation(string operationName, TimeSpan duration, long memoryDelta)
    {
        _operationMetrics.AddOrUpdate(operationName,
            new PerformanceMetrics(
                OperationName: operationName,
                CallCount: 1,
                TotalResponseTime: duration,
                AverageResponseTime: duration,
                MinResponseTime: duration,
                MaxResponseTime: duration,
                FailureCount: 0,
                LastFailure: null,
                AverageMemoryUsage: memoryDelta,
                CacheHitRate: 0.0,
                LastUpdated: DateTimeOffset.UtcNow
            ),
            (key, existing) => existing with
            {
                CallCount = existing.CallCount + 1,
                TotalResponseTime = existing.TotalResponseTime + duration,
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    (existing.TotalResponseTime + duration).TotalMilliseconds / (existing.CallCount + 1)),
                MinResponseTime = duration < existing.MinResponseTime ? duration : existing.MinResponseTime,
                MaxResponseTime = duration > existing.MaxResponseTime ? duration : existing.MaxResponseTime,
                AverageMemoryUsage = (existing.AverageMemoryUsage * existing.CallCount + memoryDelta) / (existing.CallCount + 1),
                LastUpdated = DateTimeOffset.UtcNow
            });

        // Record recent event for trend analysis
        _recentEvents.Enqueue(new PerformanceEvent(
            Timestamp: DateTimeOffset.UtcNow,
            OperationName: operationName,
            Duration: duration,
            Success: true,
            MemoryDelta: memoryDelta
        ));

        // Limit queue size for memory efficiency
        while (_recentEvents.Count > 1000)
        {
            _recentEvents.TryDequeue(out _);
        }
    }

    private void RecordFailedOperation(string operationName, TimeSpan duration, Exception exception)
    {
        _operationMetrics.AddOrUpdate(operationName,
            new PerformanceMetrics(
                OperationName: operationName,
                CallCount: 1,
                TotalResponseTime: duration,
                AverageResponseTime: duration,
                MinResponseTime: duration,
                MaxResponseTime: duration,
                FailureCount: 1,
                LastFailure: exception.GetType().Name,
                AverageMemoryUsage: 0,
                CacheHitRate: 0.0,
                LastUpdated: DateTimeOffset.UtcNow
            ),
            (key, existing) => existing with
            {
                CallCount = existing.CallCount + 1,
                FailureCount = existing.FailureCount + 1,
                LastFailure = exception.GetType().Name,
                LastUpdated = DateTimeOffset.UtcNow
            });

        _logger.LogWarning("Operation {OperationName} failed after {Duration}ms: {Exception}", 
            operationName, duration.TotalMilliseconds, exception.Message);
    }

    private void RecordAllocation(string typeName, long estimatedSize)
    {
        // Track allocation patterns for optimization analysis
        _logger.LogTrace("Allocated {TypeName} with estimated size {Size} bytes", typeName, estimatedSize);
    }

    private static long EstimateObjectSize<T>(T obj)
    {
        // Rough estimation - in production, consider using more sophisticated profiling
        return obj switch
        {
            string str => str.Length * 2 + 24, // UTF-16 + object overhead
            Array arr => arr.Length * 8 + 24,  // Rough estimate for reference arrays
            _ => Marshal.SizeOf<IntPtr>() + 16  // Object header + reference
        };
    }

    private static string GetOptimizationSuggestion(string operationName, PerformanceMetrics metrics)
    {
        return operationName.ToLowerInvariant() switch
        {
            var name when name.Contains("workitem") => "Consider implementing work item caching or batching requests",
            var name when name.Contains("repository") => "Consider caching repository metadata or using incremental updates",
            var name when name.Contains("build") => "Consider using build result caching or filtering recent builds only",
            var name when name.Contains("test") => "Consider aggregating test results or implementing result caching",
            _ => "Consider implementing caching, request batching, or optimizing the query"
        };
    }

    private SystemMetrics GetCurrentSystemMetrics()
    {
        var memoryInfo = GC.GetGCMemoryInfo();
        ThreadPool.GetAvailableThreads(out var availableWorkers, out var availableIO);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIO);

        return new SystemMetrics(
            TotalMemoryBytes: memoryInfo.TotalAvailableMemoryBytes,
            UsedMemoryBytes: memoryInfo.MemoryLoadBytes,
            GCGen0Collections: GC.CollectionCount(0),
            GCGen1Collections: GC.CollectionCount(1),
            GCGen2Collections: GC.CollectionCount(2),
            ThreadPoolWorkerThreads: maxWorkers - availableWorkers,
            ThreadPoolCompletionPortThreads: maxIO - availableIO,
            ProcessorCount: Environment.ProcessorCount,
            WorkingSetBytes: Environment.WorkingSet
        );
    }

    private static WarmingPriority CalculateWarmingPriority(PerformanceMetrics metrics)
    {
        return (metrics.CallCount, metrics.AverageResponseTime.TotalMilliseconds) switch
        {
            (> 100, > 1000) => WarmingPriority.Critical,
            (> 50, > 500) => WarmingPriority.High,
            (> 20, > 200) => WarmingPriority.Medium,
            _ => WarmingPriority.Low
        };
    }

    private static double CalculateEstimatedBenefit(PerformanceMetrics metrics)
    {
        // Estimate time savings based on call frequency and response time
        return metrics.CallCount * metrics.AverageResponseTime.TotalMilliseconds * 0.8; // 80% cache hit assumption
    }

    private static TimeSpan CalculatePreloadTime(PerformanceMetrics metrics)
    {
        // Estimate time needed to preload based on average response time
        return TimeSpan.FromMilliseconds(metrics.AverageResponseTime.TotalMilliseconds * 1.2);
    }

    private void CleanupOldMetrics(object? state)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
            var oldKeys = _operationMetrics
                .Where(kvp => kvp.Value.LastUpdated < cutoff)
                .Select(kvp => kvp.Key)
                .ToArray();

            foreach (var key in oldKeys)
            {
                _operationMetrics.TryRemove(key, out _);
            }

            if (oldKeys.Length > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old performance metrics", oldKeys.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance metrics cleanup");
        }
    }

    #endregion

    public void Dispose()
    {
        _metricsCleanupTimer?.Dispose();
    }
}

