using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Infrastructure;

namespace MemoryTestRunner;

/// <summary>
/// Simple memory usage comparison test between original and optimized cache services.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Memory Usage Comparison: Original vs Optimized Cache");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        var (originalService, optimizedService) = SetupServices();

        // Test parameters - same as original benchmark that showed 1.33MB per operation
        const int testOperations = 1024;
        var testStrings = Enumerable.Range(0, 1000)
            .Select(i => $"test_string_{i}_with_content_to_simulate_real_data")
            .ToArray();

        // Test Original Cache Service
        Console.WriteLine("Testing Original Cache Service...");
        var originalResults = await RunMemoryTest("Original", originalService, testStrings, testOperations);

        // Clear memory between tests
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Task.Delay(1000);

        // Test Optimized Cache Service
        Console.WriteLine("Testing Memory-Optimized Cache Service...");
        var optimizedResults = await RunMemoryTest("Optimized", optimizedService, testStrings, testOperations);

        // Calculate improvements
        Console.WriteLine();
        Console.WriteLine("=== COMPARISON RESULTS ===");
        Console.WriteLine();
        
        PrintComparison("Execution Time", originalResults.ExecutionTime, optimizedResults.ExecutionTime, "ms");
        PrintComparison("Memory Used", originalResults.MemoryUsed, optimizedResults.MemoryUsed, "bytes");
        PrintComparison("Memory Per Op", originalResults.MemoryPerOperation, optimizedResults.MemoryPerOperation, "bytes");
        PrintComparison("GC Gen0", originalResults.GcGen0, optimizedResults.GcGen0, "collections");
        PrintComparison("GC Gen1", originalResults.GcGen1, optimizedResults.GcGen1, "collections");

        Console.WriteLine();
        Console.WriteLine("=== OPTIMIZATION SUMMARY ===");
        var memoryReduction = originalResults.MemoryUsed - optimizedResults.MemoryUsed;
        var memoryReductionPercent = originalResults.MemoryUsed > 0 ? (memoryReduction * 100.0 / originalResults.MemoryUsed) : 0;
        Console.WriteLine($"Memory Reduction: {memoryReduction:N0} bytes ({memoryReductionPercent:F1}%)");
        Console.WriteLine($"Original Memory Per Operation: {originalResults.MemoryPerOperation / 1024.0:F2} KB");
        Console.WriteLine($"Optimized Memory Per Operation: {optimizedResults.MemoryPerOperation / 1024.0:F2} KB");

        optimizedService.Dispose();
    }

    static (ICacheService original, MemoryOptimizedCacheService optimized) SetupServices()
    {
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/test",
                ["AzureDevOps:PersonalAccessToken"] = "test-token",
                ["Caching:EnableMemoryCache"] = "true",
                ["Caching:MaxMemoryCacheSizeMB"] = "100",
                ["Caching:DefaultExpirationMinutes"] = "5"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddMemoryCache();
        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        
        services.Configure<ProductionConfiguration>(config =>
        {
            config.Caching = new CachingConfiguration
            {
                MaxMemoryCacheSizeMB = 100,
                DefaultExpirationMinutes = 5,
                EnableMemoryCache = true
            };
        });
        
        services.AddSingleton<MemoryPoolService>();
        services.AddSingleton<CacheService>();
        services.AddSingleton<MemoryOptimizedCacheService>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        return (
            serviceProvider.GetRequiredService<CacheService>(),
            serviceProvider.GetRequiredService<MemoryOptimizedCacheService>()
        );
    }

    static async Task<TestResults> RunMemoryTest(string testName, ICacheService cacheService, string[] testStrings, int operations)
    {
        // Force garbage collection before test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var gcBefore = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var memoryBefore = GC.GetTotalMemory(false);

        var stopwatch = Stopwatch.StartNew();

        // Execute the same test as the original benchmark
        var tasks = new Task[operations];
        for (int i = 0; i < operations; i++)
        {
            var index = i % testStrings.Length;
            tasks[i] = cacheService.SetAsync($"{testName.ToLower()}_{i}", testStrings[index]);
        }
        await Task.WhenAll(tasks);

        stopwatch.Stop();

        var memoryAfter = GC.GetTotalMemory(false);
        var gcAfter = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);

        var results = new TestResults
        {
            TestName = testName,
            ExecutionTime = stopwatch.ElapsedMilliseconds,
            MemoryBefore = memoryBefore,
            MemoryAfter = memoryAfter,
            MemoryUsed = memoryAfter - memoryBefore,
            MemoryPerOperation = (memoryAfter - memoryBefore) / operations,
            GcGen0 = gcAfter - gcBefore,
            GcGen1 = gc1After - gc1Before,
            Operations = operations
        };

        Console.WriteLine($"{testName} Results:");
        Console.WriteLine($"  Time: {results.ExecutionTime} ms");
        Console.WriteLine($"  Memory Used: {results.MemoryUsed:N0} bytes ({results.MemoryUsed / (1024.0 * 1024):F2} MB)");
        Console.WriteLine($"  Memory Per Op: {results.MemoryPerOperation:N0} bytes ({results.MemoryPerOperation / 1024.0:F2} KB)");
        Console.WriteLine($"  GC Gen0: {results.GcGen0}, Gen1: {results.GcGen1}");
        Console.WriteLine();

        return results;
    }

    static void PrintComparison(string metric, long original, long optimized, string unit)
    {
        var improvement = original - optimized;
        var improvementPercent = original > 0 ? (improvement * 100.0 / original) : 0;
        var factor = original > 0 ? (original / (double)optimized) : 1;

        Console.WriteLine($"{metric,-15}: {original:N0} -> {optimized:N0} {unit}");
        Console.WriteLine($"  Improvement: {improvement:N0} {unit} ({improvementPercent:F1}%) [{factor:F1}x better]");
        Console.WriteLine();
    }

    public class TestResults
    {
        public string TestName { get; set; } = "";
        public long ExecutionTime { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public long MemoryUsed { get; set; }
        public long MemoryPerOperation { get; set; }
        public int GcGen0 { get; set; }
        public int GcGen1 { get; set; }
        public int Operations { get; set; }
    }
}