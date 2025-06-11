using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Extensions;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Infrastructure;

namespace AzureDevOps.MCP.Tests.Benchmarks;

/// <summary>
/// Benchmarks comparing original vs memory-optimized cache implementations.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class MemoryOptimizationBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private ICacheService _originalCache = null!;
    private MemoryOptimizedCacheService _optimizedCache = null!;
    
    // Test data sets for different scenarios
    private readonly string[] _smallStrings = Enumerable.Range(0, 1000)
        .Select(i => $"small_value_{i}_with_some_content_to_simulate_real_data")
        .ToArray();
    
    private readonly object[] _mediumObjects = Enumerable.Range(0, 1000)
        .Select(i => new { 
            Id = i, 
            Name = $"Object_{i}", 
            Description = $"This is a medium-sized object with ID {i} containing various properties",
            Timestamp = DateTime.UtcNow,
            Tags = new[] { $"tag1_{i}", $"tag2_{i}", $"tag3_{i}" },
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = $"value1_{i}",
                ["key2"] = $"value2_{i}",
                ["key3"] = $"value3_{i}"
            }
        })
        .ToArray();
    
    private readonly byte[][] _largeByteArrays = Enumerable.Range(0, 100)
        .Select(i => new byte[5120]) // 5KB each to trigger compression
        .ToArray();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/benchmark",
                ["AzureDevOps:PersonalAccessToken"] = "benchmark-token",
                ["Caching:EnableMemoryCache"] = "true",
                ["Caching:MaxMemoryCacheSizeMB"] = "100",
                ["Caching:DefaultExpirationMinutes"] = "5"
            })
            .Build();

        // Configure both cache implementations
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddMemoryCache();
        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        
        // Configure ProductionConfiguration
        services.Configure<ProductionConfiguration>(config =>
        {
            config.Caching = new CachingConfiguration
            {
                MaxMemoryCacheSizeMB = 100,
                DefaultExpirationMinutes = 5,
                EnableMemoryCache = true
            };
        });
        
        // Add memory pool service
        services.AddSingleton<MemoryPoolService>();
        
        // Register both cache services
        services.AddSingleton<CacheService>();
        services.AddSingleton<MemoryOptimizedCacheService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get instances
        _originalCache = _serviceProvider.GetRequiredService<CacheService>();
        _optimizedCache = _serviceProvider.GetRequiredService<MemoryOptimizedCacheService>();
        
        // Initialize large byte arrays with random data
        var random = new Random(42); // Deterministic for benchmarking
        foreach (var array in _largeByteArrays)
        {
            random.NextBytes(array);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _optimizedCache?.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    // ========================================
    // ORIGINAL CACHE BENCHMARKS (BASELINE)
    // ========================================

    [Benchmark(Baseline = true)]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task Original_CacheSet_SmallStrings(int count)
    {
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            var index = i % _smallStrings.Length;
            tasks[i] = _originalCache.SetAsync($"original_small_{i}", _smallStrings[index]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task Original_CacheSet_MediumObjects(int count)
    {
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            var index = i % _mediumObjects.Length;
            tasks[i] = _originalCache.SetAsync($"original_medium_{i}", _mediumObjects[index]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    public async Task Original_CacheSet_LargeObjects(int count)
    {
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            var index = i % _largeByteArrays.Length;
            tasks[i] = _originalCache.SetAsync($"original_large_{i}", _largeByteArrays[index]);
        }
        await Task.WhenAll(tasks);
    }

    // ========================================
    // OPTIMIZED CACHE BENCHMARKS
    // ========================================

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task Optimized_CacheSet_SmallStrings(int count)
    {
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            var index = i % _smallStrings.Length;
            tasks[i] = _optimizedCache.SetAsync($"optimized_small_{i}", _smallStrings[index]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task Optimized_CacheSet_MediumObjects(int count)
    {
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            var index = i % _mediumObjects.Length;
            tasks[i] = _optimizedCache.SetAsync($"optimized_medium_{i}", _mediumObjects[index]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    public async Task Optimized_CacheSet_LargeObjects(int count)
    {
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            var index = i % _largeByteArrays.Length;
            tasks[i] = _optimizedCache.SetAsync($"optimized_large_{i}", _largeByteArrays[index]);
        }
        await Task.WhenAll(tasks);
    }

    // ========================================
    // MIXED WORKLOAD BENCHMARKS
    // ========================================

    [Benchmark]
    [Arguments(500)]
    public async Task Original_MixedWorkload(int operations)
    {
        var tasks = new Task[operations];
        var random = new Random(42);
        
        for (int i = 0; i < operations; i++)
        {
            var operation = random.Next(3);
            tasks[i] = operation switch
            {
                0 => _originalCache.SetAsync($"mixed_small_{i}", _smallStrings[i % _smallStrings.Length]),
                1 => _originalCache.SetAsync($"mixed_medium_{i}", _mediumObjects[i % _mediumObjects.Length]),
                2 => _originalCache.SetAsync($"mixed_large_{i}", _largeByteArrays[i % _largeByteArrays.Length]),
                _ => Task.CompletedTask
            };
        }
        
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(500)]
    public async Task Optimized_MixedWorkload(int operations)
    {
        var tasks = new Task[operations];
        var random = new Random(42);
        
        for (int i = 0; i < operations; i++)
        {
            var operation = random.Next(3);
            tasks[i] = operation switch
            {
                0 => _optimizedCache.SetAsync($"mixed_small_{i}", _smallStrings[i % _smallStrings.Length]),
                1 => _optimizedCache.SetAsync($"mixed_medium_{i}", _mediumObjects[i % _mediumObjects.Length]),
                2 => _optimizedCache.SetAsync($"mixed_large_{i}", _largeByteArrays[i % _largeByteArrays.Length]),
                _ => Task.CompletedTask
            };
        }
        
        await Task.WhenAll(tasks);
    }

    // ========================================
    // MEMORY PRESSURE TESTS
    // ========================================

    [Benchmark]
    public async Task Original_MemoryPressureTest()
    {
        const int iterations = 1000;
        var tasks = new Task[iterations];
        
        for (int i = 0; i < iterations; i++)
        {
            // Create varied object sizes to stress memory
            var size = (i % 3) switch
            {
                0 => _smallStrings[i % _smallStrings.Length],
                1 => _mediumObjects[i % _mediumObjects.Length],
                2 => _largeByteArrays[i % _largeByteArrays.Length],
                _ => "default"
            };
            
            tasks[i] = _originalCache.SetAsync($"pressure_original_{i}", size);
        }
        
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Optimized_MemoryPressureTest()
    {
        const int iterations = 1000;
        var tasks = new Task[iterations];
        
        for (int i = 0; i < iterations; i++)
        {
            // Create varied object sizes to stress memory
            var size = (i % 3) switch
            {
                0 => _smallStrings[i % _smallStrings.Length],
                1 => _mediumObjects[i % _mediumObjects.Length],
                2 => _largeByteArrays[i % _largeByteArrays.Length],
                _ => "default"
            };
            
            tasks[i] = _optimizedCache.SetAsync($"pressure_optimized_{i}", size);
        }
        
        await Task.WhenAll(tasks);
    }
}