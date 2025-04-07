using System.Collections.Frozen;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Extensions;
using AzureDevOps.MCP.Security;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.Services.Infrastructure;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureDevOps.MCP.Tests.Benchmarks;

/// <summary>
/// Custom benchmark configuration optimized for .NET 9 performance testing.
/// </summary>
public class PerformanceBenchmarkConfig : ManualConfig
{
	public PerformanceBenchmarkConfig ()
	{
		AddJob (Job.Default
			.WithRuntime (BenchmarkDotNet.Environments.CoreRuntime.Core90)
			.WithWarmupCount (3)
			.WithIterationCount (5)
			.WithInvocationCount (1024)); // Must be multiple of UnrollFactor (16)

		AddDiagnoser (MemoryDiagnoser.Default);
		AddDiagnoser (ThreadingDiagnoser.Default);

		AddColumn (StatisticColumn.Mean);
		AddColumn (StatisticColumn.StdDev);
		AddColumn (StatisticColumn.Median);
		AddColumn (StatisticColumn.P95);
		AddColumn (BaselineColumn.Default);
		AddColumn (TargetMethodColumn.Method);

		AddExporter (MarkdownExporter.GitHub);
		AddExporter (HtmlExporter.Default);

		AddLogger (ConsoleLogger.Default);
	}
}

/// <summary>
/// Benchmarks for caching service performance using .NET 9 optimizations.
/// </summary>
[Config (typeof (PerformanceBenchmarkConfig))]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class CacheServiceBenchmarks
{
	private IServiceProvider _serviceProvider = null!;
	private ICacheService _cacheService = null!;
	private IMemoryCache _memoryCache = null!;

	// Test data sets
	private readonly string[] _smallStrings = Enumerable.Range (0, 1000)
		.Select (i => $"small_value_{i}")
		.ToArray ();

	private readonly FrozenDictionary<string, object> _mediumObjects = Enumerable.Range (0, 1000)
		.ToDictionary (i => $"key_{i}", i => (object)new { Id = i, Name = $"Object_{i}", Timestamp = DateTime.UtcNow })
		.ToFrozenDictionary ();

	private readonly byte[][] _largeByteArrays = Enumerable.Range (0, 100)
		.Select (i => new byte[10240]) // 10KB each
		.ToArray ();

	[GlobalSetup]
	public void GlobalSetup ()
	{
		var services = new ServiceCollection ();

		var configuration = new ConfigurationBuilder ()
			.AddInMemoryCollection (new Dictionary<string, string?> {
				["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/benchmark",
				["AzureDevOps:PersonalAccessToken"] = "benchmark-token",
				["Caching:EnableMemoryCache"] = "true",
				["Caching:MaxMemoryCacheSizeMB"] = "500",
				["Caching:DefaultExpirationMinutes"] = "30"
			})
			.Build ();

		services.AddAzureDevOpsMcpServices (configuration);

		_serviceProvider = services.BuildServiceProvider ();
		_cacheService = _serviceProvider.GetRequiredService<ICacheService> ();
		_memoryCache = _serviceProvider.GetRequiredService<IMemoryCache> ();
	}

	[GlobalCleanup]
	public void GlobalCleanup ()
	{
		if (_serviceProvider is IDisposable disposable) {
			disposable.Dispose ();
		}
	}

	[Benchmark (Baseline = true)]
	[Arguments (100)]
	[Arguments (1000)]
	[Arguments (5000)]
	public async Task CacheSet_SmallStrings (int count)
	{
		var tasks = new Task[count];
		for (int i = 0; i < count; i++) {
			var index = i % _smallStrings.Length;
			tasks[i] = _cacheService.SetAsync ($"bench_small_{i}", _smallStrings[index]);
		}
		await Task.WhenAll (tasks);
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	[Arguments (5000)]
	public async Task CacheGet_SmallStrings (int count)
	{
		// Pre-populate cache
		for (int i = 0; i < count; i++) {
			var index = i % _smallStrings.Length;
			await _cacheService.SetAsync ($"bench_get_small_{i}", _smallStrings[index]);
		}

		// Benchmark retrieval
		var tasks = new Task[count];
		for (int i = 0; i < count; i++) {
			tasks[i] = _cacheService.GetAsync<string> ($"bench_get_small_{i}");
		}
		await Task.WhenAll (tasks);
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (500)]
	[Arguments (1000)]
	public async Task CacheSet_MediumObjects (int count)
	{
		var keys = _mediumObjects.Keys.Take (count).ToArray ();
		var tasks = new Task[count];

		for (int i = 0; i < count; i++) {
			var key = keys[i];
			tasks[i] = _cacheService.SetAsync ($"bench_medium_{i}", _mediumObjects[key]);
		}
		await Task.WhenAll (tasks);
	}

	[Benchmark]
	[Arguments (10)]
	[Arguments (50)]
	[Arguments (100)]
	public async Task CacheSet_LargeByteArrays (int count)
	{
		var tasks = new Task[count];
		for (int i = 0; i < count; i++) {
			var index = i % _largeByteArrays.Length;
			tasks[i] = _cacheService.SetAsync ($"bench_large_{i}", _largeByteArrays[index]);
		}
		await Task.WhenAll (tasks);
	}

	[Benchmark]
	[Arguments (50)]
	[Arguments (100)]
	[Arguments (200)]
	public async Task CacheConcurrentAccess (int concurrentTasks)
	{
		var tasks = new Task[concurrentTasks];

		for (int i = 0; i < concurrentTasks; i++) {
			var taskId = i;
			tasks[i] = Task.Run (async () => {
				// Mix of set and get operations
				await _cacheService.SetAsync ($"concurrent_{taskId}", $"value_{taskId}");
				await _cacheService.GetAsync<string> ($"concurrent_{taskId}");
				await _cacheService.RemoveAsync ($"concurrent_{taskId}");
			});
		}

		await Task.WhenAll (tasks);
	}

	[Benchmark]
	public void MemoryCache_DirectAccess_Set ()
	{
		for (int i = 0; i < 1000; i++) {
			_memoryCache.Set ($"direct_{i}", _smallStrings[i % _smallStrings.Length]);
		}
	}

	[Benchmark]
	public void MemoryCache_DirectAccess_Get ()
	{
		// Pre-populate
		for (int i = 0; i < 1000; i++) {
			_memoryCache.Set ($"direct_get_{i}", _smallStrings[i % _smallStrings.Length]);
		}

		// Benchmark
		for (int i = 0; i < 1000; i++) {
			_memoryCache.TryGetValue ($"direct_get_{i}", out _);
		}
	}
}

/// <summary>
/// Benchmarks for performance optimization service using .NET 9 features.
/// </summary>
[Config (typeof (PerformanceBenchmarkConfig))]
[MemoryDiagnoser]
public class PerformanceOptimizationBenchmarks
{
	private PerformanceOptimizationService _service = null!;
	private PerformanceConfiguration _config = null!;

	[GlobalSetup]
	public void Setup ()
	{
		var logger = new Mock<ILogger<PerformanceOptimizationService>> ();
		_config = new PerformanceConfiguration {
			SlowOperationThresholdMs = 1000,
			EnableCircuitBreaker = true,
			EnableMonitoring = true
		};

		var options = new Mock<IOptions<PerformanceConfiguration>> ();
		options.Setup (x => x.Value).Returns (_config);

		_service = new PerformanceOptimizationService (logger.Object, options.Object);
	}

	[GlobalCleanup]
	public void Cleanup ()
	{
		_service?.Dispose ();
	}

	[Benchmark (Baseline = true)]
	public TimeSpan OptimizeCacheDuration_HighFrequency ()
	{
		return _service.OptimizeCacheDuration ("GetWorkItems", "workitems");
	}

	[Benchmark]
	public TimeSpan OptimizeCacheDuration_StaticData ()
	{
		return _service.OptimizeCacheDuration ("get", "projects");
	}

	[Benchmark]
	public TimeSpan OptimizeCacheDuration_UserSpecific ()
	{
		return _service.OptimizeCacheDuration ("get", "workitems");
	}

	[Benchmark]
	[Arguments (1)]
	[Arguments (10)]
	[Arguments (100)]
	public async Task TrackPerformanceAsync_Overhead (int iterations)
	{
		var tasks = new Task[iterations];

		for (int i = 0; i < iterations; i++) {
			var opName = $"BenchmarkOperation_{i}";
			tasks[i] = _service.TrackPerformanceAsync (opName, async ct => {
				await Task.Delay (1, ct); // Simulate minimal work
				return "result";
			});
		}

		await Task.WhenAll (tasks);
	}

	[Benchmark]
	public PerformanceRecommendations GetPerformanceRecommendations ()
	{
		return _service.GetPerformanceRecommendations ();
	}

	[Benchmark]
	public CacheWarmingPlan CreateCacheWarmingPlan ()
	{
		return _service.CreateCacheWarmingPlan ();
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	[Arguments (10000)]
	public string OptimizeAllocation_StringCreation (int iterations)
	{
		string result = "";
		for (int i = 0; i < iterations; i++) {
			result = _service.OptimizeAllocation (() => $"Allocated_String_{i}");
		}
		return result;
	}
}

/// <summary>
/// Benchmarks for secret management performance.
/// </summary>
[Config (typeof (PerformanceBenchmarkConfig))]
[MemoryDiagnoser]
public class SecretManagerBenchmarks
{
	private ProductionSecretManager _secretManager = null!;
	private EnvironmentSecretManager _envSecretManager = null!;

	[GlobalSetup]
	public void Setup ()
	{
		// Setup environment variables for testing
		for (int i = 0; i < 100; i++) {
			Environment.SetEnvironmentVariable ($"AZDO_BENCHMARK_SECRET_{i}", $"secret_value_{i}");
		}

		var logger = new Mock<ILogger<ProductionSecretManager>> ();
		var envLogger = new Mock<ILogger<EnvironmentSecretManager>> ();

		var config = new ProductionConfiguration {
			Security = new ProductionSecurityConfiguration {
				EnableKeyVault = false // Use environment variables only for benchmarks
			}
		};

		var options = new Mock<IOptions<ProductionConfiguration>> ();
		options.Setup (x => x.Value).Returns (config);

		_secretManager = new ProductionSecretManager (logger.Object, options.Object);
		_envSecretManager = new EnvironmentSecretManager (envLogger.Object);
	}

	[GlobalCleanup]
	public void Cleanup ()
	{
		// Clean up environment variables
		for (int i = 0; i < 100; i++) {
			Environment.SetEnvironmentVariable ($"AZDO_BENCHMARK_SECRET_{i}", null);
		}

		_secretManager?.Dispose ();
	}

	[Benchmark (Baseline = true)]
	[Arguments (1)]
	[Arguments (10)]
	[Arguments (50)]
	public async Task ProductionSecretManager_GetSecrets (int secretCount)
	{
		var tasks = new Task[secretCount];

		for (int i = 0; i < secretCount; i++) {
			tasks[i] = _secretManager.GetSecretAsync ($"benchmark_secret_{i}");
		}

		try {
			await Task.WhenAll (tasks);
		} catch {
			// Expected for non-existent secrets in benchmark
		}
	}

	[Benchmark]
	[Arguments (1)]
	[Arguments (10)]
	[Arguments (50)]
	public async Task EnvironmentSecretManager_GetSecrets (int secretCount)
	{
		var tasks = new Task[secretCount];

		for (int i = 0; i < secretCount; i++) {
			tasks[i] = _envSecretManager.GetSecretAsync ($"benchmark_secret_{i}");
		}

		try {
			await Task.WhenAll (tasks);
		} catch {
			// Expected for non-existent secrets in benchmark
		}
	}

	[Benchmark]
	public async Task SecretExists_Performance ()
	{
		var existsResults = new bool[10];

		for (int i = 0; i < 10; i++) {
			existsResults[i] = await _secretManager.SecretExistsAsync ($"benchmark_secret_{i}");
		}
	}

	[Benchmark]
	public async Task RefreshSecrets_Performance ()
	{
		await _secretManager.RefreshSecretsAsync ();
	}
}

/// <summary>
/// Benchmarks for configuration and serialization performance.
/// </summary>
[Config (typeof (PerformanceBenchmarkConfig))]
[MemoryDiagnoser]
public class ConfigurationBenchmarks
{
	private IServiceProvider _serviceProvider = null!;
	private IConfiguration _configuration = null!;

	[GlobalSetup]
	public void Setup ()
	{
		var configData = new Dictionary<string, string?> {
			["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/benchmark",
			["AzureDevOps:PersonalAccessToken"] = "benchmark-token",
			["Caching:EnableMemoryCache"] = "true",
			["Caching:MaxMemoryCacheSizeMB"] = "200",
			["Caching:DefaultExpirationMinutes"] = "10",
			["Performance:EnableMonitoring"] = "true",
			["Performance:SlowOperationThresholdMs"] = "1000",
			["Security:EnableKeyVault"] = "false",
			["Logging:EnableStructuredLogging"] = "true"
		};

		_configuration = new ConfigurationBuilder ()
			.AddInMemoryCollection (configData)
			.Build ();

		var services = new ServiceCollection ();
		services.AddAzureDevOpsMcpServices (_configuration);
		_serviceProvider = services.BuildServiceProvider ();
	}

	[GlobalCleanup]
	public void Cleanup ()
	{
		if (_serviceProvider is IDisposable disposable) {
			disposable.Dispose ();
		}
	}

	[Benchmark (Baseline = true)]
	public ProductionConfiguration GetProductionConfiguration ()
	{
		var options = _serviceProvider.GetRequiredService<IOptions<ProductionConfiguration>> ();
		return options.Value;
	}

	[Benchmark]
	public AzureDevOpsConfiguration GetAzureDevOpsConfiguration ()
	{
		var options = _serviceProvider.GetRequiredService<IOptions<AzureDevOpsConfiguration>> ();
		return options.Value;
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	public void ConfigurationBinding_Performance (int iterations)
	{
		for (int i = 0; i < iterations; i++) {
			var config = new ProductionConfiguration ();
			_configuration.Bind (config);
		}
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	public List<string> ConfigurationValidation_Performance (int iterations)
	{
		var config = new ProductionConfiguration {
			AzureDevOps = new AzureDevOpsConfiguration {
				OrganizationUrl = "https://dev.azure.com/test",
				PersonalAccessToken = "test-token"
			}
		};

		List<string> lastResult = new ();
		for (int i = 0; i < iterations; i++) {
			lastResult = config.ValidateConfiguration ();
		}
		return lastResult;
	}
}

/// <summary>
/// Benchmarks for .NET 9 frozen collections performance.
/// </summary>
[Config (typeof (PerformanceBenchmarkConfig))]
[MemoryDiagnoser]
public class FrozenCollectionsBenchmarks
{
	private readonly Dictionary<string, string> _regularDictionary = new ();
	private readonly FrozenDictionary<string, string> _frozenDictionary;
	private readonly HashSet<string> _regularHashSet = new ();
	private readonly FrozenSet<string> _frozenHashSet;

	private readonly string[] _lookupKeys;
	private readonly string[] _testValues;

	public FrozenCollectionsBenchmarks ()
	{
		// Prepare test data
		_testValues = Enumerable.Range (0, 1000)
			.Select (i => $"value_{i}")
			.ToArray ();

		_lookupKeys = Enumerable.Range (0, 1000)
			.Select (i => $"key_{i}")
			.ToArray ();

		// Populate collections
		for (int i = 0; i < 1000; i++) {
			_regularDictionary[_lookupKeys[i]] = _testValues[i];
			_regularHashSet.Add (_lookupKeys[i]);
		}

		_frozenDictionary = _regularDictionary.ToFrozenDictionary ();
		_frozenHashSet = _regularHashSet.ToFrozenSet ();
	}

	[Benchmark (Baseline = true)]
	[Arguments (100)]
	[Arguments (1000)]
	public string Dictionary_Lookups (int lookupCount)
	{
		string result = "";
		for (int i = 0; i < lookupCount; i++) {
			var key = _lookupKeys[i % _lookupKeys.Length];
			if (_regularDictionary.TryGetValue (key, out var value)) {
				result = value;
			}
		}
		return result;
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	public string FrozenDictionary_Lookups (int lookupCount)
	{
		string result = "";
		for (int i = 0; i < lookupCount; i++) {
			var key = _lookupKeys[i % _lookupKeys.Length];
			if (_frozenDictionary.TryGetValue (key, out var value)) {
				result = value;
			}
		}
		return result;
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	public bool HashSet_Contains (int lookupCount)
	{
		bool result = false;
		for (int i = 0; i < lookupCount; i++) {
			var key = _lookupKeys[i % _lookupKeys.Length];
			result = _regularHashSet.Contains (key);
		}
		return result;
	}

	[Benchmark]
	[Arguments (100)]
	[Arguments (1000)]
	public bool FrozenSet_Contains (int lookupCount)
	{
		bool result = false;
		for (int i = 0; i < lookupCount; i++) {
			var key = _lookupKeys[i % _lookupKeys.Length];
			result = _frozenHashSet.Contains (key);
		}
		return result;
	}
}