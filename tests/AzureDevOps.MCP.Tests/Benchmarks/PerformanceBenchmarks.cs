using AzureDevOps.MCP.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AzureDevOps.MCP.Tests.Benchmarks;

[TestClass]
public class PerformanceBenchmarks
{
    private ICacheService _cacheService = null!;
    private IPerformanceService _performanceService = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        
        var provider = services.BuildServiceProvider();
        var memoryCache = provider.GetRequiredService<IMemoryCache>();
        var cacheLogger = provider.GetRequiredService<ILogger<CacheService>>();
        var perfLogger = provider.GetRequiredService<ILogger<PerformanceService>>();
        
        _cacheService = new CacheService(memoryCache, cacheLogger);
        _performanceService = new PerformanceService(perfLogger);
    }

    [TestMethod]
    public async Task Benchmark_CacheService_SetAndGet_Performance()
    {
        const int iterations = 1000;
        const string keyPrefix = "benchmark_key_";
        const string value = "benchmark_value_with_some_content_to_simulate_real_data";

        // Warmup
        for (int i = 0; i < 10; i++)
        {
            await _cacheService.SetAsync($"warmup_{i}", value);
            await _cacheService.GetAsync<string>($"warmup_{i}");
        }

        // Benchmark Set operations
        var setStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await _cacheService.SetAsync($"{keyPrefix}{i}", value);
        }
        setStopwatch.Stop();

        // Benchmark Get operations
        var getStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await _cacheService.GetAsync<string>($"{keyPrefix}{i}");
        }
        getStopwatch.Stop();

        // Assert performance expectations
        var setAvgMs = setStopwatch.ElapsedMilliseconds / (double)iterations;
        var getAvgMs = getStopwatch.ElapsedMilliseconds / (double)iterations;

        Console.WriteLine($"Cache Set: {setAvgMs:F3}ms avg, {iterations / setStopwatch.Elapsed.TotalSeconds:F0} ops/sec");
        Console.WriteLine($"Cache Get: {getAvgMs:F3}ms avg, {iterations / getStopwatch.Elapsed.TotalSeconds:F0} ops/sec");

        // Performance thresholds (adjust based on requirements)
        setAvgMs.Should().BeLessThan(1.0, "Set operations should be fast");
        getAvgMs.Should().BeLessThan(0.5, "Get operations should be very fast");
    }

    [TestMethod]
    public async Task Benchmark_PerformanceService_TrackingOverhead()
    {
        const int iterations = 10000;

        // Benchmark without tracking
        var withoutTrackingStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await SimulateWork(1); // 1ms work
        }
        withoutTrackingStopwatch.Stop();

        // Benchmark with tracking
        var withTrackingStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            using var tracker = _performanceService.TrackOperation("BenchmarkOperation");
            await SimulateWork(1); // 1ms work
        }
        withTrackingStopwatch.Stop();

        var overhead = withTrackingStopwatch.ElapsedMilliseconds - withoutTrackingStopwatch.ElapsedMilliseconds;
        var overheadPerOp = overhead / (double)iterations;

        Console.WriteLine($"Without tracking: {withoutTrackingStopwatch.ElapsedMilliseconds}ms total");
        Console.WriteLine($"With tracking: {withTrackingStopwatch.ElapsedMilliseconds}ms total");
        Console.WriteLine($"Overhead: {overhead}ms total, {overheadPerOp:F3}ms per operation");

        // Performance threshold: tracking overhead should be minimal
        overheadPerOp.Should().BeLessThan(0.1, "Performance tracking overhead should be minimal");

        var metrics = await _performanceService.GetMetricsAsync();
        metrics.TotalOperations.Should().Be(iterations);
        metrics.Operations.Should().ContainKey("BenchmarkOperation");
    }

    [TestMethod]
    public async Task Benchmark_ConcurrentOperations()
    {
        const int concurrentTasks = 50;
        const int operationsPerTask = 100;

        var tasks = new List<Task>();

        var stopwatch = Stopwatch.StartNew();

        for (int taskId = 0; taskId < concurrentTasks; taskId++)
        {
            var id = taskId; // Capture for closure
            tasks.Add(Task.Run(async () =>
            {
                for (int op = 0; op < operationsPerTask; op++)
                {
                    var key = $"concurrent_{id}_{op}";
                    var value = $"value_{id}_{op}";
                    
                    using var tracker = _performanceService.TrackOperation("ConcurrentOperation");
                    await _cacheService.SetAsync(key, value);
                    var retrieved = await _cacheService.GetAsync<string>(key);
                    
                    if (retrieved != value)
                    {
                        throw new InvalidOperationException($"Cache consistency error: {key}");
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var totalOperations = concurrentTasks * operationsPerTask;
        var opsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"Concurrent operations: {totalOperations} in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {opsPerSecond:F0} operations/second");

        // Verify metrics consistency
        var metrics = await _performanceService.GetMetricsAsync();
        metrics.Operations["ConcurrentOperation"].Count.Should().Be(totalOperations);
        
        // Performance threshold
        opsPerSecond.Should().BeGreaterThan(1000, "Should handle at least 1000 concurrent operations per second");
    }

    [TestMethod]
    public async Task Benchmark_MemoryUsage()
    {
        const int dataItems = 10000;
        const string largeValue = new string('X', 1000); // 1KB per item

        var initialMemory = GC.GetTotalMemory(true);

        // Add lots of data to cache
        for (int i = 0; i < dataItems; i++)
        {
            await _cacheService.SetAsync($"memory_test_{i}", largeValue);
        }

        var afterCacheMemory = GC.GetTotalMemory(false);
        var cacheMemoryUsage = afterCacheMemory - initialMemory;

        // Clear cache
        _cacheService.Clear();
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var afterClearMemory = GC.GetTotalMemory(true);
        var memoryReclaimed = afterCacheMemory - afterClearMemory;

        Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");
        Console.WriteLine($"After caching {dataItems} items: {afterCacheMemory:N0} bytes");
        Console.WriteLine($"Cache memory usage: {cacheMemoryUsage:N0} bytes ({cacheMemoryUsage / 1024.0 / 1024.0:F2} MB)");
        Console.WriteLine($"After clear: {afterClearMemory:N0} bytes");
        Console.WriteLine($"Memory reclaimed: {memoryReclaimed:N0} bytes ({memoryReclaimed / (double)cacheMemoryUsage * 100:F1}%)");

        // Verify memory is reclaimed efficiently
        var reclaimPercentage = memoryReclaimed / (double)cacheMemoryUsage * 100;
        reclaimPercentage.Should().BeGreaterThan(80, "Cache should release most of its memory when cleared");
    }

    private static async Task SimulateWork(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }
}