using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Diagnosers;

namespace AzureDevOps.MCP.Tests.Benchmarks;

/// <summary>
/// Main program to run BenchmarkDotNet performance benchmarks.
/// Usage: dotnet run -c Release --project tests/AzureDevOps.MCP.Tests --filter Benchmark
/// </summary>
public class BenchmarkRunner
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Azure DevOps MCP Performance Benchmarks");
        Console.WriteLine("=======================================");
        
        if (args.Length == 0)
        {
            RunAllBenchmarks();
        }
        else
        {
            RunSpecificBenchmark(args[0]);
        }
    }

    /// <summary>
    /// Runs all available benchmark suites.
    /// </summary>
    static void RunAllBenchmarks()
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(CsvExporter.Default)
            .AddLogger(ConsoleLogger.Default);

        Console.WriteLine("Running all benchmark suites...\n");

        // Run each benchmark class
        var benchmarkTypes = new[]
        {
            typeof(CacheServiceBenchmarks),
            typeof(MemoryOptimizationBenchmarks),
            typeof(PerformanceOptimizationBenchmarks),
            typeof(SecretManagerBenchmarks),
            typeof(ConfigurationBenchmarks),
            typeof(FrozenCollectionsBenchmarks)
        };

        foreach (var benchmarkType in benchmarkTypes)
        {
            Console.WriteLine($"Running {benchmarkType.Name}...");
            BenchmarkDotNet.Running.BenchmarkRunner.Run(benchmarkType, config);
            Console.WriteLine($"Completed {benchmarkType.Name}\n");
        }

        Console.WriteLine("All benchmarks completed!");
        Console.WriteLine("Results exported to:");
        Console.WriteLine("- BenchmarkDotNet.Artifacts/results/");
        Console.WriteLine("- HTML and Markdown reports generated");
    }

    /// <summary>
    /// Runs a specific benchmark by name.
    /// </summary>
    static void RunSpecificBenchmark(string benchmarkName)
    {
        var config = DefaultConfig.Instance;

        switch (benchmarkName.ToLowerInvariant())
        {
            case "cache":
            case "cacheservice":
                Console.WriteLine("Running Cache Service Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheServiceBenchmarks>(config);
                break;

            case "performance":
            case "optimization":
                Console.WriteLine("Running Performance Optimization Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<PerformanceOptimizationBenchmarks>(config);
                break;

            case "secret":
            case "secrets":
                Console.WriteLine("Running Secret Manager Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<SecretManagerBenchmarks>(config);
                break;

            case "config":
            case "configuration":
                Console.WriteLine("Running Configuration Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<ConfigurationBenchmarks>(config);
                break;

            case "frozen":
            case "collections":
                Console.WriteLine("Running Frozen Collections Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<FrozenCollectionsBenchmarks>(config);
                break;

            case "memory":
            case "memopt":
                Console.WriteLine("Running Simple Memory Benchmark...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<SimpleBenchmark>(config);
                break;

            default:
                Console.WriteLine($"Unknown benchmark: {benchmarkName}");
                Console.WriteLine("Available benchmarks:");
                Console.WriteLine("- cache (CacheServiceBenchmarks)");
                Console.WriteLine("- performance (PerformanceOptimizationBenchmarks)");
                Console.WriteLine("- secrets (SecretManagerBenchmarks)");
                Console.WriteLine("- config (ConfigurationBenchmarks)");
                Console.WriteLine("- frozen (FrozenCollectionsBenchmarks)");
                break;
        }
    }
}

/// <summary>
/// Extension class for running benchmark programs.
/// </summary>
public static class BenchmarkProgram
{
    /// <summary>
    /// Quick benchmark runner for testing specific scenarios.
    /// </summary>
    public static void RunQuickBenchmark()
    {
        Console.WriteLine("Running quick performance benchmark...");
        
        // Run a subset of benchmarks for quick testing
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator) // Allow debug builds for testing
            .AddLogger(ConsoleLogger.Default);

        BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheServiceBenchmarks>(config);
    }

    /// <summary>
    /// Runs memory-focused benchmarks.
    /// </summary>
    public static void RunMemoryBenchmarks()
    {
        Console.WriteLine("Running memory allocation benchmarks...");
        
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddLogger(ConsoleLogger.Default);

        // Focus on memory-intensive benchmarks
        BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheServiceBenchmarks>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<FrozenCollectionsBenchmarks>(config);
    }

    /// <summary>
    /// Runs concurrency-focused benchmarks.
    /// </summary>
    public static void RunConcurrencyBenchmarks()
    {
        Console.WriteLine("Running concurrency benchmarks...");
        
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddDiagnoser(ThreadingDiagnoser.Default)
            .AddExporter(HtmlExporter.Default)
            .AddLogger(ConsoleLogger.Default);

        // Focus on concurrent operations
        BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheServiceBenchmarks>(config);
        BenchmarkDotNet.Running.BenchmarkRunner.Run<SecretManagerBenchmarks>(config);
    }
}