using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Security;
using AzureDevOps.MCP.Services.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Comprehensive health check for Azure DevOps connectivity.
/// </summary>
public class AzureDevOpsHealthCheck : IHealthCheck
{
    readonly IAzureDevOpsConnectionFactory _connectionFactory;
    readonly ILogger<AzureDevOpsHealthCheck> _logger;
    readonly HealthCheckConfiguration _config;

    public AzureDevOpsHealthCheck(
        IAzureDevOpsConnectionFactory connectionFactory,
        ILogger<AzureDevOpsHealthCheck> logger,
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
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCancellation.Token);

            // Test basic connectivity
            var connection = await _connectionFactory.GetConnectionAsync(combinedCancellation.Token);
            data["connection_established"] = true;

            // Test API responsiveness by getting current user
            try
            {
                var identityClient = connection.GetClient<Microsoft.VisualStudio.Services.Identity.Client.IdentityHttpClient>();
                var identity = await identityClient.GetCurrentIdentityAsync(combinedCancellation.Token);
                
                data["api_responsive"] = true;
                data["user_authenticated"] = identity != null;
                data["user_id"] = identity?.Id?.ToString() ?? "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure DevOps API test failed");
                data["api_responsive"] = false;
                data["api_error"] = ex.Message;
            }

            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;

            var isHealthy = (bool)data["connection_established"] && (bool)data.GetValueOrDefault("api_responsive", false);
            var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;
            var description = isHealthy ? "Azure DevOps is accessible and responsive" : "Azure DevOps connectivity issues detected";

            return new HealthCheckResult(status, description, data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, "Health check was cancelled", data: data);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["timeout_seconds"] = _config.TimeoutSeconds;
            
            return new HealthCheckResult(HealthStatus.Unhealthy, "Azure DevOps connection timeout", data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;

            _logger.LogError(ex, "Azure DevOps health check failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, "Azure DevOps connectivity failed", ex, data);
        }
    }
}

/// <summary>
/// Health check for cache system (memory and/or Redis).
/// </summary>
public class CacheHealthCheck : IHealthCheck
{
    readonly ICacheService _cacheService;
    readonly ILogger<CacheHealthCheck> _logger;
    readonly HealthCheckConfiguration _config;

    public CacheHealthCheck(
        ICacheService cacheService,
        ILogger<CacheHealthCheck> logger,
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
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCancellation.Token);

            // Test cache write/read operations
            var testKey = $"health-check-{Guid.NewGuid():N}";
            var testValue = $"test-value-{DateTime.UtcNow:O}";
            var testExpiration = TimeSpan.FromMinutes(1);

            // Write test
            await _cacheService.SetAsync(testKey, testValue, testExpiration, combinedCancellation.Token);
            data["write_successful"] = true;

            // Read test
            var retrievedValue = await _cacheService.GetAsync<string>(testKey, combinedCancellation.Token);
            var readSuccessful = retrievedValue == testValue;
            data["read_successful"] = readSuccessful;

            // Cleanup test key
            try
            {
                await _cacheService.RemoveAsync(testKey, combinedCancellation.Token);
                data["cleanup_successful"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup health check cache key {Key}", testKey);
                data["cleanup_successful"] = false;
            }

            // Get cache statistics if available
            if (_cacheService is CacheService concreteCache)
            {
                try
                {
                    var stats = concreteCache.GetStatistics();
                    data["cache_statistics"] = new
                    {
                        hit_count = stats.HitCount,
                        miss_count = stats.MissCount,
                        hit_ratio = stats.HitRatio,
                        eviction_count = stats.EvictionCount,
                        memory_usage_bytes = stats.MemoryUsageBytes
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve cache statistics");
                }
            }

            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;

            var isHealthy = (bool)data["write_successful"] && readSuccessful;
            var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            var description = isHealthy ? "Cache system is working correctly" : "Cache system has issues";

            return new HealthCheckResult(status, description, data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, "Cache health check was cancelled", data: data);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            
            return new HealthCheckResult(HealthStatus.Unhealthy, "Cache operation timeout", data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;

            _logger.LogError(ex, "Cache health check failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, "Cache system failed", ex, data);
        }
    }
}

/// <summary>
/// Health check for memory usage and system resources.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    readonly ILogger<MemoryHealthCheck> _logger;
    readonly HealthCheckConfiguration _config;

    public MemoryHealthCheck(
        ILogger<MemoryHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value?.HealthChecks ?? throw new ArgumentNullException(nameof(config));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // Get memory information
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            
            data["gc_total_memory_bytes"] = totalMemory;
            data["working_set_bytes"] = workingSet;
            data["processor_count"] = Environment.ProcessorCount;

            // Get GC information
            data["gc_gen0_collections"] = GC.CollectionCount(0);
            data["gc_gen1_collections"] = GC.CollectionCount(1);
            data["gc_gen2_collections"] = GC.CollectionCount(2);

            // Calculate memory pressure
            var availableMemory = GetAvailableMemory();
            if (availableMemory.HasValue)
            {
                data["available_memory_bytes"] = availableMemory.Value;
                var memoryUsagePercent = (1.0 - (double)availableMemory.Value / workingSet) * 100;
                data["memory_usage_percent"] = Math.Round(memoryUsagePercent, 2);
                
                var isMemoryHealthy = memoryUsagePercent < _config.MemoryThresholdPercent;
                data["memory_healthy"] = isMemoryHealthy;

                if (!isMemoryHealthy)
                {
                    var description = $"Memory usage is {memoryUsagePercent:F1}%, exceeding threshold of {_config.MemoryThresholdPercent}%";
                    return Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, description, data: data));
                }
            }

            // Get thread pool information
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            
            data["threadpool_available_worker_threads"] = availableWorkerThreads;
            data["threadpool_available_completion_port_threads"] = availableCompletionPortThreads;
            data["threadpool_max_worker_threads"] = maxWorkerThreads;
            data["threadpool_max_completion_port_threads"] = maxCompletionPortThreads;

            var workerThreadUsagePercent = (1.0 - (double)availableWorkerThreads / maxWorkerThreads) * 100;
            data["threadpool_usage_percent"] = Math.Round(workerThreadUsagePercent, 2);

            // Check if thread pool is under pressure
            if (workerThreadUsagePercent > 80)
            {
                var description = $"Thread pool usage is {workerThreadUsagePercent:F1}%, indicating high load";
                return Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, description, data: data));
            }

            return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, "Memory and system resources are healthy", data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;
            
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "Memory health check failed", ex, data));
        }
    }

    /// <summary>
    /// Gets available memory in bytes (platform-specific implementation).
    /// </summary>
    static long? GetAvailableMemory()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsAvailableMemory();
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetLinuxAvailableMemory();
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    static long? GetWindowsAvailableMemory()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "OS get TotalVisibleMemorySize,FreePhysicalMemory /value",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var freeMemoryLine = lines.FirstOrDefault(l => l.StartsWith("FreePhysicalMemory="));
            
            if (freeMemoryLine != null && long.TryParse(freeMemoryLine.Split('=')[1].Trim(), out var freeMemoryKB))
            {
                return freeMemoryKB * 1024; // Convert from KB to bytes
            }
        }
        catch
        {
            // Fallback to performance counter approach or return null
        }

        return null;
    }

    static long? GetLinuxAvailableMemory()
    {
        try
        {
            if (File.Exists("/proc/meminfo"))
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                var availableLine = lines.FirstOrDefault(l => l.StartsWith("MemAvailable:"));
                
                if (availableLine != null)
                {
                    var parts = availableLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && long.TryParse(parts[1], out var availableKB))
                    {
                        return availableKB * 1024; // Convert from KB to bytes
                    }
                }
            }
        }
        catch
        {
            // Return null if we can't read memory info
        }

        return null;
    }
}

/// <summary>
/// Health check for secrets management system.
/// </summary>
public class SecretsHealthCheck : IHealthCheck
{
    readonly ISecretManager _secretManager;
    readonly ILogger<SecretsHealthCheck> _logger;
    readonly HealthCheckConfiguration _config;

    readonly string[] _requiredSecrets = { "OrganizationUrl", "PersonalAccessToken" };

    public SecretsHealthCheck(
        ISecretManager secretManager,
        ILogger<SecretsHealthCheck> logger,
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
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCancellation.Token);

            var secretResults = new Dictionary<string, bool>();

            // Check each required secret
            foreach (var secretName in _requiredSecrets)
            {
                try
                {
                    var exists = await _secretManager.SecretExistsAsync(secretName, combinedCancellation.Token);
                    secretResults[secretName] = exists;
                    
                    if (!exists)
                    {
                        _logger.LogWarning("Required secret {SecretName} is not available", secretName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check secret {SecretName}", secretName);
                    secretResults[secretName] = false;
                }
            }

            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["secret_availability"] = secretResults;
            data["secrets_checked"] = _requiredSecrets.Length;

            var availableSecretsCount = secretResults.Count(kv => kv.Value);
            var allSecretsAvailable = availableSecretsCount == _requiredSecrets.Length;
            
            data["available_secrets_count"] = availableSecretsCount;
            data["all_secrets_available"] = allSecretsAvailable;

            if (allSecretsAvailable)
            {
                return new HealthCheckResult(HealthStatus.Healthy, "All required secrets are available", data: data);
            }
            else if (availableSecretsCount > 0)
            {
                var missingSecrets = secretResults.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
                data["missing_secrets"] = missingSecrets;
                
                var description = $"Some required secrets are missing: {string.Join(", ", missingSecrets)}";
                return new HealthCheckResult(HealthStatus.Degraded, description, data: data);
            }
            else
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, "No required secrets are available", data: data);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, "Secrets health check was cancelled", data: data);
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            
            return new HealthCheckResult(HealthStatus.Unhealthy, "Secrets check timeout", data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["response_time_ms"] = stopwatch.ElapsedMilliseconds;
            data["error"] = ex.Message;
            data["error_type"] = ex.GetType().Name;

            _logger.LogError(ex, "Secrets health check failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, "Secrets system failed", ex, data);
        }
    }
}

/// <summary>
/// Composite health check that provides overall application health status.
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    readonly ILogger<ApplicationHealthCheck> _logger;
    readonly EnvironmentConfiguration _envConfig;

    public ApplicationHealthCheck(
        ILogger<ApplicationHealthCheck> logger,
        IOptions<ProductionConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _envConfig = config?.Value?.Environment ?? throw new ArgumentNullException(nameof(config));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["environment"] = _envConfig.Name,
            ["version"] = _envConfig.Version,
            ["build"] = _envConfig.Build ?? "unknown",
            ["instance_id"] = _envConfig.InstanceId,
            ["deployed_at"] = _envConfig.DeployedAt?.ToString("O") ?? "unknown",
            ["uptime"] = Environment.TickCount64,
            ["machine_name"] = Environment.MachineName,
            ["process_id"] = Environment.ProcessId,
            ["framework_version"] = Environment.Version.ToString(),
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        var description = $"Application is running in {_envConfig.Name} environment";
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, description, data: data));
    }
}