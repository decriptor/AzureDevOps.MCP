using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Security;
using AzureDevOps.MCP.Services.Infrastructure;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Modern health check for Azure DevOps connectivity using .NET 9 features.
/// </summary>
public sealed class ModernAzureDevOpsHealthCheck : IHealthCheck
{
    private readonly IAzureDevOpsConnectionFactory _connectionFactory;
    private readonly ILogger<ModernAzureDevOpsHealthCheck> _logger;
    private readonly HealthCheckConfiguration _config;

    public ModernAzureDevOpsHealthCheck(
        IAzureDevOpsConnectionFactory connectionFactory,
        ILogger<ModernAzureDevOpsHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value?.HealthChecks ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("AzureDevOpsHealthCheck");
        activity.Start();
        
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Test basic connectivity using modern async patterns
            var connection = await _connectionFactory.GetConnectionAsync(combinedCts.Token);
            data["connection_established"] = true;
            data["connection_type"] = connection.GetType().Name;

            // Test API responsiveness with proper error handling
            try
            {
                // Use a simple projects API call to test connectivity
                var projectClient = connection.GetClient<Microsoft.TeamFoundation.Core.WebApi.ProjectHttpClient>();
                var projects = await projectClient.GetProjects(top: 1);
                
                data["api_responsive"] = true;
                data["projects_accessible"] = projects?.Any() == true;
                data["api_test_method"] = "GetProjects";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure DevOps API test failed");
                data["api_responsive"] = false;
                data["api_error"] = ex.Message;
                data["api_error_type"] = ex.GetType().Name;
            }

            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            var isHealthy = (bool)data["connection_established"] && 
                           (bool)data.GetValueOrDefault("api_responsive", false);
            
            var status = isHealthy 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy 
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded;
            
            var description = isHealthy 
                ? "Azure DevOps is accessible and responsive" 
                : "Azure DevOps connectivity issues detected";

            return new HealthCheckResult(status, description, data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            data["cancelled_by"] = "external_cancellation";
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Health check was cancelled", 
                data: data);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["timeout_seconds"] = _config.TimeoutSeconds;
            data["timeout_reason"] = "operation_timeout";
            
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Azure DevOps connection timeout", 
                data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;
            data["stack_trace"] = ex.StackTrace;

            _logger.LogError(ex, "Azure DevOps health check failed");
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Azure DevOps connectivity failed", 
                ex, 
                data);
        }
    }
}

/// <summary>
/// Modern cache health check using .NET 9 performance features.
/// </summary>
public sealed class ModernCacheHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<ModernCacheHealthCheck> _logger;
    private readonly HealthCheckConfiguration _config;
    
    // Use frozen dictionary for better performance
    private static readonly FrozenDictionary<string, string> TestData = new Dictionary<string, string>
    {
        ["test-key"] = "test-value",
        ["health-check"] = "cache-validation"
    }.ToFrozenDictionary();

    public ModernCacheHealthCheck(
        ICacheService cacheService,
        ILogger<ModernCacheHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value?.HealthChecks ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("CacheHealthCheck");
        activity.Start();
        
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Generate unique test key using .NET 9 string interpolation improvements
            var testKey = $"health-check-{Guid.NewGuid():N}-{DateTimeOffset.UtcNow.Ticks}";
            var testValue = $"test-value-{DateTimeOffset.UtcNow:O}";
            var testExpiration = TimeSpan.FromMinutes(1);

            // Test cache operations with modern async patterns
            var writeStopwatch = Stopwatch.StartNew();
            await _cacheService.SetAsync(testKey, testValue, testExpiration, combinedCts.Token);
            writeStopwatch.Stop();
            data["write_successful"] = true;
            data["write_duration_ms"] = writeStopwatch.ElapsedMilliseconds;

            var readStopwatch = Stopwatch.StartNew();
            var retrievedValue = await _cacheService.GetAsync<string>(testKey, combinedCts.Token);
            readStopwatch.Stop();
            var readSuccessful = string.Equals(retrievedValue, testValue, StringComparison.Ordinal);
            data["read_successful"] = readSuccessful;
            data["read_duration_ms"] = readStopwatch.ElapsedMilliseconds;
            data["value_matches"] = readSuccessful;

            // Cleanup test key
            try
            {
                await _cacheService.RemoveAsync(testKey, combinedCts.Token);
                data["cleanup_successful"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup health check cache key {Key}", testKey);
                data["cleanup_successful"] = false;
                data["cleanup_error"] = ex.Message;
            }

            // Get cache statistics if available (using pattern matching)
            if (_cacheService is CacheService { } concreteCache)
            {
                try
                {
                    var stats = concreteCache.GetStatistics();
                    data["cache_statistics"] = new
                    {
                        hit_count = stats.HitCount,
                        miss_count = stats.MissCount,
                        hit_ratio = stats.HitRatio,
                        total_operations = stats.HitCount + stats.MissCount
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve cache statistics");
                    data["statistics_error"] = ex.Message;
                }
            }

            stopwatch.Stop();
            data["total_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            var isHealthy = (bool)data["write_successful"] && readSuccessful;
            var status = isHealthy 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy 
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;
            var description = isHealthy 
                ? "Cache system is working correctly" 
                : "Cache system has issues";

            return new HealthCheckResult(status, description, data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Cache health check was cancelled", 
                data: data);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            data["total_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["timeout_seconds"] = _config.TimeoutSeconds;
            
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Cache operation timeout", 
                data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["total_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;

            _logger.LogError(ex, "Cache health check failed");
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Cache system failed", 
                ex, 
                data: data);
        }
    }
}

/// <summary>
/// Modern memory health check using .NET 9 performance improvements.
/// </summary>
public sealed class ModernMemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<ModernMemoryHealthCheck> _logger;
    private readonly HealthCheckConfiguration _config;
    
    // Use frozen set for performance
    private static readonly FrozenSet<string> CriticalProcesses = new HashSet<string>
    {
        "dotnet",
        "AzureDevOps.MCP"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public ModernMemoryHealthCheck(
        ILogger<ModernMemoryHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value?.HealthChecks ?? throw new ArgumentNullException(nameof(config));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("MemoryHealthCheck");
        activity.Start();
        
        var data = new Dictionary<string, object>();

        try
        {
            // Use .NET 9 improved GC APIs
            var memoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            
            data["gc_total_memory_bytes"] = totalMemory;
            data["gc_heap_size_bytes"] = memoryInfo.HeapSizeBytes;
            data["gc_memory_load_bytes"] = memoryInfo.MemoryLoadBytes;
            data["gc_total_available_memory_bytes"] = memoryInfo.TotalAvailableMemoryBytes;
            data["gc_high_memory_load_threshold_bytes"] = memoryInfo.HighMemoryLoadThresholdBytes;
            data["working_set_bytes"] = workingSet;
            data["processor_count"] = Environment.ProcessorCount;

            // Enhanced GC information with .NET 9 features
            data["gc_gen0_collections"] = GC.CollectionCount(0);
            data["gc_gen1_collections"] = GC.CollectionCount(1);
            data["gc_gen2_collections"] = GC.CollectionCount(2);
            data["gc_total_collections"] = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            
            // Memory pressure calculation using modern APIs
            var memoryPressurePercent = memoryInfo.TotalAvailableMemoryBytes > 0
                ? Math.Round((double)memoryInfo.MemoryLoadBytes / memoryInfo.TotalAvailableMemoryBytes * 100, 2)
                : 0.0;
            
            data["memory_pressure_percent"] = memoryPressurePercent;
            data["memory_pressure_threshold"] = _config.MemoryThresholdPercent;
            data["is_high_memory_load"] = memoryInfo.MemoryLoadBytes > memoryInfo.HighMemoryLoadThresholdBytes;

            // Thread pool information with modern patterns
            var (availableWorkerThreads, availableCompletionPortThreads) = ThreadPool.AvailableCount;
            var (maxWorkerThreads, maxCompletionPortThreads) = ThreadPool.ThreadCount;
            
            data["threadpool_available_worker_threads"] = availableWorkerThreads;
            data["threadpool_available_completion_port_threads"] = availableCompletionPortThreads;
            data["threadpool_max_worker_threads"] = maxWorkerThreads;
            data["threadpool_max_completion_port_threads"] = maxCompletionPortThreads;

            var workerThreadUsagePercent = maxWorkerThreads > 0
                ? Math.Round((1.0 - (double)availableWorkerThreads / maxWorkerThreads) * 100, 2)
                : 0.0;
            data["threadpool_usage_percent"] = workerThreadUsagePercent;

            // Process information
            using var currentProcess = Process.GetCurrentProcess();
            data["process_id"] = currentProcess.Id;
            data["process_name"] = currentProcess.ProcessName;
            data["process_start_time"] = currentProcess.StartTime.ToString("O");
            data["process_total_processor_time"] = currentProcess.TotalProcessorTime.TotalMilliseconds;

            // Health assessment with modern conditional logic
            var healthStatus = (memoryPressurePercent, workerThreadUsagePercent) switch
            {
                (< 80, < 80) => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                (< 90, < 90) => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                _ => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
            };

            var description = healthStatus switch
            {
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => 
                    "Memory and system resources are healthy",
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => 
                    $"System under pressure: Memory {memoryPressurePercent:F1}%, Threads {workerThreadUsagePercent:F1}%",
                _ => 
                    $"System critical: Memory {memoryPressurePercent:F1}%, Threads {workerThreadUsagePercent:F1}%"
            };

            data["health_status"] = healthStatus.ToString();
            data["timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            return Task.FromResult(new HealthCheckResult(healthStatus, description, data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;
            data["timestamp"] = DateTimeOffset.UtcNow.ToString("O");
            
            return Task.FromResult(new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Memory health check failed", 
                ex, 
                data));
        }
    }
}

/// <summary>
/// Modern secrets health check using .NET 9 async improvements.
/// </summary>
public sealed class ModernSecretsHealthCheck : IHealthCheck
{
    private readonly ISecretManager _secretManager;
    private readonly ILogger<ModernSecretsHealthCheck> _logger;
    private readonly HealthCheckConfiguration _config;

    // Use frozen set for required secrets
    private static readonly FrozenSet<string> RequiredSecrets = new HashSet<string>
    {
        "OrganizationUrl",
        "PersonalAccessToken"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public ModernSecretsHealthCheck(
        ISecretManager secretManager,
        ILogger<ModernSecretsHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _secretManager = secretManager ?? throw new ArgumentNullException(nameof(secretManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value?.HealthChecks ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("SecretsHealthCheck");
        activity.Start();
        
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Check secrets concurrently using modern async patterns
            var secretTasks = RequiredSecrets
                .Select(async secretName =>
                {
                    try
                    {
                        var exists = await _secretManager.SecretExistsAsync(secretName, combinedCts.Token);
                        return new { Name = secretName, Exists = exists, Error = (string?)null };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to check secret {SecretName}", secretName);
                        return new { Name = secretName, Exists = false, Error = ex.Message };
                    }
                })
                .ToArray();

            var secretResults = await Task.WhenAll(secretTasks);

            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["secrets_checked"] = RequiredSecrets.Count;
            data["timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            // Process results using modern LINQ and pattern matching
            var availableSecrets = secretResults.Where(r => r.Exists).ToArray();
            var failedSecrets = secretResults.Where(r => !r.Exists).ToArray();
            var errorSecrets = secretResults.Where(r => r.Error is not null).ToArray();

            data["available_secrets_count"] = availableSecrets.Length;
            data["failed_secrets_count"] = failedSecrets.Length;
            data["error_secrets_count"] = errorSecrets.Length;
            data["all_secrets_available"] = availableSecrets.Length == RequiredSecrets.Count;

            // Create detailed secret status using modern collection expressions
            data["secret_status"] = secretResults.ToDictionary(
                r => r.Name,
                r => new { exists = r.Exists, error = r.Error }
            );

            if (failedSecrets.Length > 0)
            {
                data["missing_secrets"] = failedSecrets.Select(s => s.Name).ToArray();
            }

            if (errorSecrets.Length > 0)
            {
                data["error_secrets"] = errorSecrets.Select(s => new { s.Name, s.Error }).ToArray();
            }

            // Health status determination using switch expression
            var (status, description) = (availableSecrets.Length, failedSecrets.Length, errorSecrets.Length) switch
            {
                var (available, failed, errors) when available == RequiredSecrets.Count =>
                    (Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, 
                     "All required secrets are available"),
                
                var (available, failed, errors) when available > 0 =>
                    (Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, 
                     $"Some required secrets are missing: {string.Join(", ", failedSecrets.Select(s => s.Name))}"),
                
                _ =>
                    (Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                     "No required secrets are available")
            };

            return new HealthCheckResult(status, description, data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Secrets health check was cancelled", 
                data: data);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["timeout_seconds"] = _config.TimeoutSeconds;
            
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Secrets check timeout", 
                data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;

            _logger.LogError(ex, "Secrets health check failed");
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Secrets system failed", 
                ex, 
                data: data);
        }
    }
}

/// <summary>
/// Modern application health check using .NET 9 features.
/// </summary>
public sealed class ModernApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ModernApplicationHealthCheck> _logger;
    private readonly EnvironmentConfiguration _envConfig;
    
    // Use frozen dictionary for build info
    private static readonly FrozenDictionary<string, object> BuildInfo = new Dictionary<string, object>
    {
        ["framework_version"] = Environment.Version.ToString(),
        ["runtime_version"] = RuntimeInformation.FrameworkDescription,
        ["os_version"] = RuntimeInformation.OSDescription,
        ["architecture"] = RuntimeInformation.OSArchitecture.ToString(),
        ["process_architecture"] = RuntimeInformation.ProcessArchitecture.ToString()
    }.ToFrozenDictionary();

    public ModernApplicationHealthCheck(
        ILogger<ModernApplicationHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _envConfig = config?.Value?.Environment ?? throw new ArgumentNullException(nameof(config));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("ApplicationHealthCheck");
        activity.Start();
        
        try
        {
            var uptime = Environment.TickCount64;
            var uptimeSpan = TimeSpan.FromMilliseconds(uptime);
            
            // Use modern collection expressions and improved string interpolation
            var data = new Dictionary<string, object>
            {
                ["environment"] = _envConfig.Name,
                ["version"] = _envConfig.Version,
                ["build"] = _envConfig.Build ?? "unknown",
                ["instance_id"] = _envConfig.InstanceId,
                ["deployed_at"] = _envConfig.DeployedAt?.ToString("O") ?? "unknown",
                ["uptime_ms"] = uptime,
                ["uptime_formatted"] = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s",
                ["machine_name"] = Environment.MachineName,
                ["process_id"] = Environment.ProcessId,
                ["user_name"] = Environment.UserName,
                ["command_line"] = Environment.CommandLine,
                ["current_directory"] = Environment.CurrentDirectory,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["timezone"] = TimeZoneInfo.Local.Id,
                ["culture"] = CultureInfo.CurrentCulture.Name,
                ["ui_culture"] = CultureInfo.CurrentUICulture.Name
            };

            // Add build information
            foreach (var (key, value) in BuildInfo)
            {
                data[key] = value;
            }

            // Add feature flags
            data["features"] = new
            {
                development_features_enabled = _envConfig.EnableDevelopmentFeatures,
                debug_endpoints_enabled = _envConfig.EnableDebugEndpoints,
                metrics_endpoints_enabled = _envConfig.EnableMetricsEndpoints
            };

            // Environment-specific health status
            var status = _envConfig.Name.ToLowerInvariant() switch
            {
                "production" => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                "staging" => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                "development" => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                _ => Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded
            };

            var description = $"Application is running in {_envConfig.Name} environment (uptime: {uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m)";
            
            return Task.FromResult(new HealthCheckResult(status, description, data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");
            
            var errorData = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };
            
            return Task.FromResult(new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
                "Application health check failed", 
                ex, 
                errorData));
        }
    }
}