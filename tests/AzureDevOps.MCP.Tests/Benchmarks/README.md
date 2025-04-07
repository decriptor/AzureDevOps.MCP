# Performance Benchmarks

This directory contains comprehensive BenchmarkDotNet performance benchmarks for the Azure DevOps MCP project, leveraging .NET 9 performance optimizations.

## Available Benchmark Suites

### 1. CacheServiceBenchmarks
Tests caching performance with different data sizes and concurrency patterns:
- Small string caching (100-5000 items)
- Medium object caching with FrozenDictionary
- Large byte array caching (10KB items)
- Concurrent access patterns
- Direct MemoryCache vs wrapped CacheService

### 2. PerformanceOptimizationBenchmarks
Tests the PerformanceOptimizationService using .NET 9 features:
- Cache duration optimization algorithms
- Performance tracking overhead measurement
- Memory allocation optimization
- Recommendation generation performance

### 3. SecretManagerBenchmarks
Tests secret management performance:
- Production vs Environment secret managers
- Parallel secret retrieval
- Secret existence checking
- Secret refresh operations

### 4. ConfigurationBenchmarks
Tests configuration and serialization performance:
- Configuration binding performance
- Configuration validation overhead
- Service provider resolution
- Configuration object creation

### 5. FrozenCollectionsBenchmarks
Tests .NET 9 frozen collections vs traditional collections:
- FrozenDictionary vs Dictionary lookups
- FrozenSet vs HashSet contains operations
- Performance at different scales (100-1000 items)

## Running Benchmarks

### Prerequisites
- .NET 9 SDK
- Release build configuration (required for accurate benchmarks)

### Run All Benchmarks
```bash
# From the solution root
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs
```

### Run Specific Benchmark Suite
```bash
# Cache benchmarks
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs cache

# Performance optimization benchmarks
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs performance

# Secret manager benchmarks
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs secrets

# Configuration benchmarks
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs config

# Frozen collections benchmarks
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs frozen
```

### Quick Development Testing
```bash
# Run subset of benchmarks for quick feedback (allows debug builds)
dotnet run --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs quick
```

### Memory-Focused Benchmarks
```bash
# Focus on memory allocation patterns
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs memory
```

### Concurrency-Focused Benchmarks
```bash
# Focus on threading and concurrent operations
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs concurrency
```

## Benchmark Results

Results are automatically exported to:
- `BenchmarkDotNet.Artifacts/results/` - Raw benchmark data
- `*.html` - Interactive HTML reports with charts
- `*.md` - GitHub-flavored markdown reports
- `*.csv` - CSV data for analysis

## Key Performance Features Tested

### .NET 9 Optimizations
- **FrozenDictionary/FrozenSet**: High-performance read-only collections
- **Modern record types**: Value equality and with expressions
- **GC Memory Analysis**: Using GetGCMemoryInfo() API
- **ThreadPool Improvements**: Proper API usage patterns

### Cache Performance
- Memory cache vs custom cache service overhead
- Concurrent access patterns and thread safety
- Large object caching and memory pressure
- Cache hit/miss ratio impact

### Secret Management
- Azure Key Vault vs environment variable performance
- Parallel secret retrieval efficiency
- Caching and refresh mechanisms

### Configuration
- Configuration binding and validation overhead
- Service provider resolution performance
- Large configuration object creation

## Benchmark Configuration

The benchmarks use optimized settings for .NET 9:
- **Runtime**: .NET 9.0 (Core90)
- **Warmup**: 3 iterations
- **Measurement**: 5 iterations with 1000 invocations
- **Diagnostics**: Memory and threading diagnostics enabled
- **Statistics**: Mean, StdDev, Median, P95, Ratio columns
- **Exports**: GitHub Markdown, HTML, CSV formats

## Performance Thresholds

The benchmarks help establish performance baselines:
- Cache operations should be sub-millisecond
- Secret retrieval should handle 100+ concurrent requests
- Configuration binding should scale linearly
- Frozen collections should outperform regular collections for reads

## Continuous Performance Monitoring

Integrate these benchmarks into CI/CD:
```bash
# Add to build pipeline
dotnet run -c Release --project tests/AzureDevOps.MCP.Tests/Benchmarks/BenchmarkRunner.cs --exporters json
```

Compare results over time to detect performance regressions and validate optimizations.