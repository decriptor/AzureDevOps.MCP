using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Infrastructure;

namespace AzureDevOps.MCP.Tests.Benchmarks;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, iterationCount: 3)]
public class SimpleBenchmark
{
    private IServiceProvider _serviceProvider = null!;
    private ICacheService _originalCache = null!;
    private MemoryOptimizedCacheService _optimizedCache = null!;
    
    private readonly string[] _testStrings = Enumerable.Range(0, 100)
        .Select(i => $"test_string_{i}_with_content_to_simulate_real_data")
        .ToArray();

    [GlobalSetup]
    public void GlobalSetup()
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
        
        _serviceProvider = services.BuildServiceProvider();
        _originalCache = _serviceProvider.GetRequiredService<CacheService>();
        _optimizedCache = _serviceProvider.GetRequiredService<MemoryOptimizedCacheService>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _optimizedCache?.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task Original_Cache_512Operations()
    {
        var tasks = new Task[512];
        for (int i = 0; i < 512; i++)
        {
            var index = i % _testStrings.Length;
            tasks[i] = _originalCache.SetAsync($"original_{i}", _testStrings[index]);
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Optimized_Cache_512Operations()
    {
        var tasks = new Task[512];
        for (int i = 0; i < 512; i++)
        {
            var index = i % _testStrings.Length;
            tasks[i] = _optimizedCache.SetAsync($"optimized_{i}", _testStrings[index]);
        }
        await Task.WhenAll(tasks);
    }
}