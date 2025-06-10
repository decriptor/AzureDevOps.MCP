using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Production-ready configuration with comprehensive validation and environment support.
/// </summary>
public class ProductionConfiguration
{
    /// <summary>
    /// Azure DevOps configuration section.
    /// </summary>
    [Required]
    public AzureDevOpsConfiguration AzureDevOps { get; set; } = new()
    {
        OrganizationUrl = "",
        PersonalAccessToken = ""
    };

    /// <summary>
    /// Caching configuration for performance optimization.
    /// </summary>
    public CachingConfiguration Caching { get; set; } = new();

    /// <summary>
    /// Security configuration for production environments.
    /// </summary>
    public ProductionSecurityConfiguration Security { get; set; } = new();

    /// <summary>
    /// Performance and monitoring configuration.
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// Logging configuration for observability.
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// Rate limiting configuration to prevent abuse.
    /// </summary>
    public RateLimitingConfiguration RateLimiting { get; set; } = new();

    /// <summary>
    /// Health check configuration for monitoring.
    /// </summary>
    public HealthCheckConfiguration HealthChecks { get; set; } = new();

    /// <summary>
    /// Environment-specific settings.
    /// </summary>
    [Required]
    public EnvironmentConfiguration Environment { get; set; } = new();
}

/// <summary>
/// Enhanced Azure DevOps configuration with production features.
/// </summary>
public class EnhancedAzureDevOpsConfiguration : AzureDevOpsConfiguration
{
    /// <summary>
    /// Connection timeout in seconds (default: 30).
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Request timeout in seconds (default: 60).
    /// </summary>
    [Range(10, 600)]
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum retry attempts for failed requests (default: 3).
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base retry delay in milliseconds (default: 1000).
    /// </summary>
    [Range(100, 10000)]
    public int BaseRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum concurrent requests per organization (default: 10).
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Enable request compression (default: true).
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// User agent string for API requests.
    /// </summary>
    public string UserAgent { get; set; } = "AzureDevOps-MCP/1.0";
}

/// <summary>
/// Caching configuration for different cache layers.
/// </summary>
public class CachingConfiguration
{
    /// <summary>
    /// Enable in-memory caching (default: true).
    /// </summary>
    public bool EnableMemoryCache { get; set; } = true;

    /// <summary>
    /// Enable distributed caching with Redis (default: false).
    /// </summary>
    public bool EnableDistributedCache { get; set; } = false;

    /// <summary>
    /// Redis connection string for distributed caching.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Maximum memory cache size in MB (default: 100).
    /// </summary>
    [Range(10, 1000)]
    public int MaxMemoryCacheSizeMB { get; set; } = 100;

    /// <summary>
    /// Default cache expiration in minutes (default: 5).
    /// </summary>
    [Range(1, 1440)]
    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Cache key prefix for this instance.
    /// </summary>
    public string KeyPrefix { get; set; } = "azdo-mcp";

    /// <summary>
    /// Enable cache statistics collection (default: true).
    /// </summary>
    public bool EnableStatistics { get; set; } = true;
}

/// <summary>
/// Security configuration for production deployments.
/// </summary>
public class ProductionSecurityConfiguration
{
    /// <summary>
    /// Enable Azure Key Vault for secrets management.
    /// </summary>
    public bool EnableKeyVault { get; set; } = false;

    /// <summary>
    /// Azure Key Vault URL.
    /// </summary>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Managed identity client ID for Key Vault access.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Enable API key authentication.
    /// </summary>
    public bool EnableApiKeyAuth { get; set; } = false;

    /// <summary>
    /// Valid API keys for authentication (stored as hashes).
    /// </summary>
    public List<string> ApiKeyHashes { get; set; } = new();

    /// <summary>
    /// Enable IP whitelisting.
    /// </summary>
    public bool EnableIpWhitelist { get; set; } = false;

    /// <summary>
    /// Allowed IP addresses or CIDR ranges.
    /// </summary>
    public List<string> AllowedIpRanges { get; set; } = new();

    /// <summary>
    /// Enable request signing for additional security.
    /// </summary>
    public bool EnableRequestSigning { get; set; } = false;

    /// <summary>
    /// CORS origins for web access.
    /// </summary>
    public List<string> CorsOrigins { get; set; } = new();
}

/// <summary>
/// Performance monitoring and optimization configuration.
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Enable performance monitoring (default: true).
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Slow operation threshold in milliseconds (default: 1000).
    /// </summary>
    [Range(100, 60000)]
    public int SlowOperationThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Very slow operation threshold in milliseconds (default: 5000).
    /// </summary>
    [Range(1000, 300000)]
    public int VerySlowOperationThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Maximum operation history entries to keep (default: 1000).
    /// </summary>
    [Range(100, 10000)]
    public int MaxOperationHistoryEntries { get; set; } = 1000;

    /// <summary>
    /// Enable memory pressure monitoring.
    /// </summary>
    public bool EnableMemoryPressureMonitoring { get; set; } = true;

    /// <summary>
    /// Memory pressure threshold percentage (default: 80).
    /// </summary>
    [Range(50, 95)]
    public int MemoryPressureThresholdPercent { get; set; } = 80;

    /// <summary>
    /// Enable circuit breaker pattern.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Circuit breaker failure threshold (default: 5).
    /// </summary>
    [Range(3, 20)]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker timeout in seconds (default: 60).
    /// </summary>
    [Range(30, 600)]
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Comprehensive logging configuration.
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level (default: Information).
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Enable structured logging with JSON format.
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Enable console logging.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Enable file logging.
    /// </summary>
    public bool EnableFileLogging { get; set; } = false;

    /// <summary>
    /// Log file path (when file logging is enabled).
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Maximum log file size in MB (default: 100).
    /// </summary>
    [Range(10, 1000)]
    public int MaxLogFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Number of log files to retain (default: 10).
    /// </summary>
    [Range(1, 50)]
    public int RetainedLogFileCount { get; set; } = 10;

    /// <summary>
    /// Enable sensitive data filtering in logs.
    /// </summary>
    public bool EnableSensitiveDataFiltering { get; set; } = true;

    /// <summary>
    /// Patterns to filter from logs (regex patterns).
    /// </summary>
    public List<string> SensitiveDataPatterns { get; set; } = new()
    {
        @"pat_[a-zA-Z0-9]{52}",  // Azure DevOps PAT
        @"Authorization:\s*Bearer\s+[a-zA-Z0-9\-._~+/]+=*",  // Bearer tokens
        @"password['""][^'""]+['""]",  // Password fields
        @"secret['""][^'""]+['""]"     // Secret fields
    };
}

/// <summary>
/// Rate limiting configuration to prevent abuse.
/// </summary>
public class RateLimitingConfiguration
{
    /// <summary>
    /// Enable rate limiting (default: true).
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Requests per minute per client (default: 60).
    /// </summary>
    [Range(1, 1000)]
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Requests per hour per client (default: 1000).
    /// </summary>
    [Range(10, 10000)]
    public int RequestsPerHour { get; set; } = 1000;

    /// <summary>
    /// Burst size for token bucket algorithm (default: 10).
    /// </summary>
    [Range(1, 100)]
    public int BurstSize { get; set; } = 10;

    /// <summary>
    /// Enable rate limiting by IP address.
    /// </summary>
    public bool EnableIpRateLimit { get; set; } = true;

    /// <summary>
    /// Enable rate limiting by API key.
    /// </summary>
    public bool EnableApiKeyRateLimit { get; set; } = true;

    /// <summary>
    /// Rate limit storage type (Memory, Redis).
    /// </summary>
    public string StorageType { get; set; } = "Memory";
}

/// <summary>
/// Health check configuration for monitoring and alerting.
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Enable health checks (default: true).
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check endpoint path (default: /health).
    /// </summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Enable detailed health check responses.
    /// </summary>
    public bool EnableDetailedResponse { get; set; } = false;

    /// <summary>
    /// Health check timeout in seconds (default: 30).
    /// </summary>
    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable Azure DevOps connectivity check.
    /// </summary>
    public bool EnableAzureDevOpsCheck { get; set; } = true;

    /// <summary>
    /// Enable cache connectivity check.
    /// </summary>
    public bool EnableCacheCheck { get; set; } = true;

    /// <summary>
    /// Enable database connectivity check (if applicable).
    /// </summary>
    public bool EnableDatabaseCheck { get; set; } = false;

    /// <summary>
    /// Enable memory usage check.
    /// </summary>
    public bool EnableMemoryCheck { get; set; } = true;

    /// <summary>
    /// Memory usage threshold for health check (default: 90%).
    /// </summary>
    [Range(50, 95)]
    public int MemoryThresholdPercent { get; set; } = 90;
}

/// <summary>
/// Environment-specific configuration.
/// </summary>
public class EnvironmentConfiguration
{
    /// <summary>
    /// Environment name (Development, Staging, Production).
    /// </summary>
    [Required]
    public string Name { get; set; } = "Production";

    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Build number or commit hash.
    /// </summary>
    public string? Build { get; set; }

    /// <summary>
    /// Deployment timestamp.
    /// </summary>
    public DateTime? DeployedAt { get; set; }

    /// <summary>
    /// Instance identifier for tracking.
    /// </summary>
    public string InstanceId { get; set; } = Environment.MachineName;

    /// <summary>
    /// Enable development features.
    /// </summary>
    public bool EnableDevelopmentFeatures { get; set; } = false;

    /// <summary>
    /// Enable debug endpoints.
    /// </summary>
    public bool EnableDebugEndpoints { get; set; } = false;

    /// <summary>
    /// Enable metrics endpoints.
    /// </summary>
    public bool EnableMetricsEndpoints { get; set; } = true;

    /// <summary>
    /// Data protection key ring path.
    /// </summary>
    public string? DataProtectionKeyPath { get; set; }

    /// <summary>
    /// Custom configuration values for environment-specific needs.
    /// </summary>
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}

/// <summary>
/// Configuration validation extensions for production readiness.
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates the production configuration and returns validation results.
    /// </summary>
    public static List<string> ValidateConfiguration(this ProductionConfiguration config)
    {
        var errors = new List<string>();

        // Azure DevOps validation
        if (string.IsNullOrWhiteSpace(config.AzureDevOps.OrganizationUrl))
            errors.Add("Azure DevOps Organization URL is required");

        if (string.IsNullOrWhiteSpace(config.AzureDevOps.PersonalAccessToken))
            errors.Add("Azure DevOps Personal Access Token is required");

        if (!Uri.TryCreate(config.AzureDevOps.OrganizationUrl, UriKind.Absolute, out _))
            errors.Add("Azure DevOps Organization URL must be a valid absolute URL");

        // Security validation
        if (config.Security.EnableKeyVault && string.IsNullOrWhiteSpace(config.Security.KeyVaultUrl))
            errors.Add("Key Vault URL is required when Key Vault is enabled");

        if (config.Security.EnableApiKeyAuth && !config.Security.ApiKeyHashes.Any())
            errors.Add("At least one API key hash is required when API key authentication is enabled");

        // Caching validation
        if (config.Caching.EnableDistributedCache && string.IsNullOrWhiteSpace(config.Caching.RedisConnectionString))
            errors.Add("Redis connection string is required when distributed caching is enabled");

        // Environment validation
        if (string.IsNullOrWhiteSpace(config.Environment.Name))
            errors.Add("Environment name is required");

        var validEnvironments = new[] { "Development", "Staging", "Production" };
        if (!validEnvironments.Contains(config.Environment.Name))
            errors.Add($"Environment name must be one of: {string.Join(", ", validEnvironments)}");

        return errors;
    }

    /// <summary>
    /// Throws an exception if the configuration is invalid.
    /// </summary>
    public static void ThrowIfInvalid(this ProductionConfiguration config)
    {
        var errors = config.ValidateConfiguration();
        if (errors.Any())
        {
            throw new InvalidOperationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
        }
    }
}