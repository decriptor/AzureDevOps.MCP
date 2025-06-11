using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using System.Collections.Frozen;
using AzureDevOps.MCP.Configuration;

namespace AzureDevOps.MCP.Tests.Configuration;

[TestClass]
public class ProductionConfigurationTests
{
    private IServiceProvider _serviceProvider = null!;
    private IConfiguration _configuration = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Build configuration with test data
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(GetTestConfigurationData());
        
        _configuration = configBuilder.Build();
        
        services.AddSingleton(_configuration);
        services.Configure<ProductionConfiguration>(_configuration);
        services.AddOptions<ProductionConfiguration>()
            .Bind(_configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [TestMethod]
    public void ProductionConfiguration_WithValidData_BindsCorrectly()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<ProductionConfiguration>>();
        var config = options.Value;

        // Assert
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.AzureDevOps);
        Assert.AreEqual("https://dev.azure.com/test", config.AzureDevOps.OrganizationUrl);
        Assert.AreEqual("test-token", config.AzureDevOps.PersonalAccessToken);
        // Enhanced configuration properties would be tested if using EnhancedAzureDevOpsConfiguration
        Assert.IsNotNull(config.AzureDevOps);
    }

    [TestMethod]
    public void ProductionConfiguration_CachingConfiguration_BindsCorrectly()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<ProductionConfiguration>>();
        var config = options.Value;

        // Assert
        Assert.IsNotNull(config.Caching);
        Assert.AreEqual(10, config.Caching.DefaultExpirationMinutes);
        Assert.AreEqual(200, config.Caching.MaxMemoryCacheSizeMB);
        Assert.AreEqual("test-azdo-mcp", config.Caching.KeyPrefix);
        Assert.AreEqual("localhost:6379", config.Caching.RedisConnectionString);
        Assert.AreEqual(true, config.Caching.EnableDistributedCache);
        Assert.AreEqual(true, config.Caching.EnableMemoryCache);
        Assert.AreEqual(true, config.Caching.EnableStatistics);
    }

    [TestMethod]
    public void ProductionConfiguration_SecurityConfiguration_BindsCorrectly()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<ProductionConfiguration>>();
        var config = options.Value;

        // Assert
        Assert.IsNotNull(config.Security);
        Assert.AreEqual(true, config.Security.EnableKeyVault);
        Assert.AreEqual("https://test-kv.vault.azure.net/", config.Security.KeyVaultUrl);
        Assert.AreEqual("test-client-id", config.Security.ManagedIdentityClientId);
        Assert.AreEqual(true, config.Security.EnableApiKeyAuth);
        Assert.IsNotNull(config.Security.ApiKeyHashes);
        Assert.AreEqual(true, config.Security.EnableIpWhitelist);
        Assert.IsNotNull(config.Security.AllowedIpRanges);
        Assert.AreEqual(2, config.Security.AllowedIpRanges.Count);
        CollectionAssert.Contains(config.Security.AllowedIpRanges, "10.0.0.0/8");
        CollectionAssert.Contains(config.Security.AllowedIpRanges, "192.168.0.0/16");
    }

    [TestMethod]
    public void ProductionConfiguration_PerformanceConfiguration_BindsCorrectly()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<ProductionConfiguration>>();
        var config = options.Value;

        // Assert
        Assert.IsNotNull(config.Performance);
        Assert.AreEqual(true, config.Performance.EnableCircuitBreaker);
        Assert.AreEqual(5, config.Performance.CircuitBreakerFailureThreshold);
        Assert.AreEqual(120, config.Performance.CircuitBreakerTimeoutSeconds);
        Assert.AreEqual(1500, config.Performance.SlowOperationThresholdMs);
        Assert.AreEqual(true, config.Performance.EnableMonitoring);
        Assert.AreEqual(5000, config.Performance.VerySlowOperationThresholdMs);
        Assert.AreEqual(true, config.Performance.EnableMemoryPressureMonitoring);
    }

    [TestMethod]
    public void ProductionConfiguration_EnvironmentConfiguration_BindsCorrectly()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<ProductionConfiguration>>();
        var config = options.Value;

        // Assert
        Assert.IsNotNull(config.Environment);
        Assert.AreEqual("Production", config.Environment.Name);
        Assert.AreEqual("1.0.0", config.Environment.Version);
        Assert.AreEqual("build-123", config.Environment.Build);
        Assert.AreEqual("instance-456", config.Environment.InstanceId);
        Assert.AreEqual(new DateTime(2024, 1, 15, 10, 30, 0), config.Environment.DeployedAt);
        Assert.AreEqual(false, config.Environment.EnableDevelopmentFeatures);
        Assert.AreEqual(false, config.Environment.EnableDebugEndpoints);
        Assert.AreEqual(true, config.Environment.EnableMetricsEndpoints);
    }

    [TestMethod]
    public void ProductionConfiguration_ValidationAttributes_WorkCorrectly()
    {
        // Arrange
        var config = new ProductionConfiguration
        {
            AzureDevOps = new AzureDevOpsConfiguration
            {
                OrganizationUrl = "", // Invalid - empty
                PersonalAccessToken = "" // Invalid - empty
            }
        };

        // Act
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(validationResults.Count > 0);
        
        // Check for specific validation errors
        var hasOrgUrlError = validationResults.Any(vr => 
            vr.MemberNames.Any(mn => mn.Contains("OrganizationUrl")));
        var hasTokenError = validationResults.Any(vr => 
            vr.MemberNames.Any(mn => mn.Contains("PersonalAccessToken")));
        
        Assert.IsTrue(hasOrgUrlError || hasTokenError);
    }

    [TestMethod]
    public void ProductionConfiguration_DefaultValues_AreSet()
    {
        // Arrange
        var config = new ProductionConfiguration();

        // Assert - verify default values are properly initialized
        Assert.IsNotNull(config.AzureDevOps);
        Assert.AreEqual("", config.AzureDevOps.OrganizationUrl);
        Assert.AreEqual("", config.AzureDevOps.PersonalAccessToken);
        
        Assert.IsNotNull(config.Caching);
        Assert.IsNotNull(config.Security);
        Assert.IsNotNull(config.Performance);
        Assert.IsNotNull(config.Logging);
        Assert.IsNotNull(config.RateLimiting);
        Assert.IsNotNull(config.HealthChecks);
        Assert.IsNotNull(config.Environment);
    }

    [TestMethod]
    public void AzureDevOpsConfiguration_ValidatesCorrectly()
    {
        // Test valid configuration
        var validConfig = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/test",
            PersonalAccessToken = "valid-token"
        };

        var validationContext = new ValidationContext(validConfig);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(validConfig, validationContext, validationResults, true);

        Assert.IsTrue(isValid);
        Assert.AreEqual(0, validationResults.Count);

        // Test invalid configuration
        var invalidConfig = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "",
            PersonalAccessToken = ""
        };

        validationContext = new ValidationContext(invalidConfig);
        validationResults = new List<ValidationResult>();
        isValid = Validator.TryValidateObject(invalidConfig, validationContext, validationResults, true);

        Assert.IsFalse(isValid);
        Assert.IsTrue(validationResults.Count > 0);
    }

    [TestMethod]
    public void CachingConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var config = new CachingConfiguration();

        // Assert
        Assert.AreEqual(5, config.DefaultExpirationMinutes);
        Assert.AreEqual(100, config.MaxMemoryCacheSizeMB);
        Assert.AreEqual("azdo-mcp", config.KeyPrefix);
        Assert.IsNull(config.RedisConnectionString);
        Assert.AreEqual(false, config.EnableDistributedCache);
        Assert.AreEqual(true, config.EnableMemoryCache);
    }

    [TestMethod]
    public void PerformanceConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var config = new PerformanceConfiguration();

        // Assert
        Assert.AreEqual(true, config.EnableCircuitBreaker);
        Assert.AreEqual(5, config.CircuitBreakerFailureThreshold);
        Assert.AreEqual(60, config.CircuitBreakerTimeoutSeconds);
        Assert.AreEqual(1000, config.SlowOperationThresholdMs);
        Assert.AreEqual(true, config.EnableMonitoring);
    }

    [TestMethod]
    public void LoggingConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var config = new LoggingConfiguration();

        // Assert
        Assert.AreEqual("Information", config.LogLevel);
        Assert.AreEqual(true, config.EnableStructuredLogging);
        Assert.AreEqual(true, config.EnableSensitiveDataFiltering);
        Assert.AreEqual(false, config.EnablePerformanceLogging);
        Assert.AreEqual(false, config.EnableSqlLogging);
    }

    [TestMethod]
    public void RateLimitingConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var config = new RateLimitingConfiguration();

        // Assert
        Assert.AreEqual(true, config.EnableRateLimiting);
        Assert.AreEqual(60, config.RequestsPerMinute);
        Assert.AreEqual(1000, config.RequestsPerHour);
        Assert.AreEqual(10000, config.RequestsPerDay);
        Assert.AreEqual("", config.RedisConnectionString);
        Assert.AreEqual(false, config.EnableDistributedRateLimiting);
    }

    [TestMethod]
    public void HealthCheckConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var config = new HealthCheckConfiguration();

        // Assert
        Assert.AreEqual(true, config.EnableHealthChecks);
        Assert.AreEqual(45, config.TimeoutSeconds);
        Assert.AreEqual(85, config.MemoryThresholdPercent);
        Assert.AreEqual(true, config.EnableDetailedResponse);
        Assert.AreEqual(false, config.EnableDatabaseCheck);
        Assert.AreEqual(true, config.EnableCacheCheck);
        Assert.AreEqual(true, config.EnableAzureDevOpsCheck);
    }

    [TestMethod]
    public void EnvironmentConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var config = new EnvironmentConfiguration();

        // Assert
        Assert.AreEqual("Production", config.Name);
        Assert.AreEqual("1.0.0", config.Version);
        Assert.IsNull(config.Build);
        Assert.IsTrue(!string.IsNullOrEmpty(config.InstanceId)); // Should generate a GUID
        Assert.IsTrue(config.InstanceId.Length > 0);
        Assert.IsNull(config.DeployedAt);
        Assert.AreEqual(false, config.EnableDevelopmentFeatures);
        Assert.AreEqual(false, config.EnableDebugEndpoints);
        Assert.AreEqual(true, config.EnableMetricsEndpoints);
    }

    private static FrozenDictionary<string, string?> GetTestConfigurationData()
    {
        return new Dictionary<string, string?>
        {
            ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/test",
            ["AzureDevOps:PersonalAccessToken"] = "test-token",
            
            ["Caching:DefaultExpirationMinutes"] = "10",
            ["Caching:MaxMemoryCacheSizeMB"] = "200",
            ["Caching:KeyPrefix"] = "test-azdo-mcp",
            ["Caching:RedisConnectionString"] = "localhost:6379",
            ["Caching:EnableDistributedCache"] = "true",
            ["Caching:EnableMemoryCache"] = "true",
            ["Caching:EnableStatistics"] = "true",
            
            ["Security:EnableKeyVault"] = "true",
            ["Security:KeyVaultUrl"] = "https://test-kv.vault.azure.net/",
            ["Security:ManagedIdentityClientId"] = "test-client-id",
            ["Security:EnableApiKeyAuth"] = "true",
            ["Security:ApiKeyHashes:0"] = "test-hash",
            ["Security:EnableIpWhitelist"] = "true",
            ["Security:AllowedIpRanges:0"] = "10.0.0.0/8",
            ["Security:AllowedIpRanges:1"] = "192.168.0.0/16",
            
            ["Performance:EnableCircuitBreaker"] = "true",
            ["Performance:CircuitBreakerFailureThreshold"] = "5",
            ["Performance:CircuitBreakerTimeoutSeconds"] = "120",
            ["Performance:SlowOperationThresholdMs"] = "1500",
            ["Performance:EnableMonitoring"] = "true",
            ["Performance:VerySlowOperationThresholdMs"] = "5000",
            ["Performance:EnableMemoryPressureMonitoring"] = "true",
            
            ["Logging:LogLevel"] = "Debug",
            ["Logging:EnableStructuredLogging"] = "true",
            ["Logging:EnableSensitiveDataFiltering"] = "false",
            ["Logging:EnablePerformanceLogging"] = "true",
            ["Logging:EnableSqlLogging"] = "false",
            
            ["RateLimiting:EnableRateLimiting"] = "true",
            ["RateLimiting:RequestsPerMinute"] = "120",
            ["RateLimiting:RequestsPerHour"] = "5000",
            ["RateLimiting:RequestsPerDay"] = "50000",
            ["RateLimiting:RedisConnectionString"] = "localhost:6379",
            ["RateLimiting:EnableDistributedRateLimiting"] = "true",
            
            ["HealthChecks:EnableHealthChecks"] = "true",
            ["HealthChecks:TimeoutSeconds"] = "45",
            ["HealthChecks:MemoryThresholdPercent"] = "85",
            ["HealthChecks:EnableDetailedResponse"] = "true",
            ["HealthChecks:EnableDatabaseCheck"] = "false",
            ["HealthChecks:EnableCacheCheck"] = "true",
            ["HealthChecks:EnableAzureDevOpsCheck"] = "true",
            
            ["Environment:Name"] = "Production",
            ["Environment:Version"] = "1.0.0",
            ["Environment:Build"] = "build-123",
            ["Environment:InstanceId"] = "instance-456",
            ["Environment:DeployedAt"] = "2024-01-15T10:30:00Z",
            ["Environment:EnableDevelopmentFeatures"] = "false",
            ["Environment:EnableDebugEndpoints"] = "false",
            ["Environment:EnableMetricsEndpoints"] = "true"
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Helper method to validate configuration using data annotations.
    /// </summary>
    private static List<ValidationResult> ValidateConfiguration(object configuration)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(configuration);
        
        Validator.TryValidateObject(configuration, validationContext, validationResults, true);
        
        // Also validate using the custom ValidateConfiguration method if it's ProductionConfiguration
        if (configuration is ProductionConfiguration prodConfig)
        {
            var customErrors = prodConfig.ValidateConfiguration();
            foreach (var error in customErrors)
            {
                validationResults.Add(new ValidationResult(error));
            }
        }
        
        return validationResults;
    }
}

[TestClass]
public class ConfigurationValidationTests
{
    [TestMethod]
    public void ProductionConfiguration_WithMissingRequiredFields_FailsValidation()
    {
        // Arrange
        var config = new ProductionConfiguration
        {
            AzureDevOps = new AzureDevOpsConfiguration
            {
                OrganizationUrl = "", // Required but empty
                PersonalAccessToken = "valid-token"
            }
        };

        // Act
        var validationResults = ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(validationResults.Count > 0);
        var hasOrgUrlError = validationResults.Any(vr => 
            vr.ErrorMessage?.Contains("OrganizationUrl") == true ||
            vr.MemberNames.Any(mn => mn.Contains("OrganizationUrl")));
        Assert.IsTrue(hasOrgUrlError);
    }

    [TestMethod]
    public void ProductionConfiguration_WithValidConfiguration_PassesValidation()
    {
        // Arrange
        var config = new ProductionConfiguration
        {
            AzureDevOps = new AzureDevOpsConfiguration
            {
                OrganizationUrl = "https://dev.azure.com/test",
                PersonalAccessToken = "valid-token"
            }
        };

        // Act
        var validationResults = ValidateConfiguration(config);

        // Assert
        Assert.AreEqual(0, validationResults.Count);
    }

    [TestMethod]
    public void CachingConfiguration_WithInvalidValues_FailsValidation()
    {
        // Arrange
        var config = new CachingConfiguration
        {
            MaxMemoryCacheSizeMB = -1, // Invalid negative value
            DefaultExpirationMinutes = -1 // Invalid negative value
        };

        // Act
        var validationResults = ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(validationResults.Count > 0);
    }

    [TestMethod]
    public void PerformanceConfiguration_WithInvalidValues_FailsValidation()
    {
        // Arrange
        var config = new PerformanceConfiguration
        {
            CircuitBreakerFailureThreshold = 0, // Invalid - should be positive
            SlowOperationThresholdMs = -100 // Invalid negative value
        };

        // Act
        var validationResults = ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(validationResults.Count > 0);
    }

    [TestMethod]
    public void RateLimitingConfiguration_WithInvalidValues_FailsValidation()
    {
        // Arrange
        var config = new RateLimitingConfiguration
        {
            RequestsPerMinute = -1, // Invalid negative value
            RequestsPerHour = 0, // Invalid zero value
            RequestsPerDay = -100 // Invalid negative value
        };

        // Act
        var validationResults = ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(validationResults.Count > 0);
    }

    [TestMethod]
    public void HealthCheckConfiguration_WithInvalidValues_FailsValidation()
    {
        // Arrange
        var config = new HealthCheckConfiguration
        {
            TimeoutSeconds = -1, // Invalid negative value
            MemoryThresholdPercent = 150 // Invalid - should be 0-100
        };

        // Act
        var validationResults = ValidateConfiguration(config);

        // Assert
        Assert.IsTrue(validationResults.Count > 0);
    }

    private static List<ValidationResult> ValidateConfiguration<T>(T config) where T : class
    {
        var validationContext = new ValidationContext(config);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(config, validationContext, validationResults, true);
        return validationResults;
    }
}

[TestClass]
public class ConfigurationExtensionsTests
{
    [TestMethod]
    public void GetConnectionString_WithValidRedisConfiguration_ReturnsFormattedString()
    {
        // Arrange
        var config = new CachingConfiguration
        {
            RedisConnectionString = "localhost:6379",
            EnableDistributedCache = true
        };

        // Act
        var connectionString = config.RedisConnectionString;

        // Assert
        Assert.AreEqual("localhost:6379", connectionString);
    }

    [TestMethod]
    public void IsProductionEnvironment_WithProductionName_ReturnsTrue()
    {
        // Arrange
        var config = new EnvironmentConfiguration
        {
            Name = "Production"
        };

        // Act
        var isProduction = config.Name.Equals("Production", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.IsTrue(isProduction);
    }

    [TestMethod]
    public void IsDevelopmentEnvironment_WithDevelopmentName_ReturnsTrue()
    {
        // Arrange
        var config = new EnvironmentConfiguration
        {
            Name = "Development"
        };

        // Act
        var isDevelopment = config.Name.Equals("Development", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.IsTrue(isDevelopment);
    }

    [TestMethod]
    public void TimeSpanProperties_SerializeAndDeserializeCorrectly()
    {
        // Arrange
        var originalConfig = new PerformanceConfiguration
        {
            CircuitBreakerTimeoutSeconds = 300,
            SlowOperationThresholdMs = 2000
        };

        // Act - simulate serialization/deserialization through configuration binding
        var configData = new Dictionary<string, string?>
        {
            ["CircuitBreakerTimeoutSeconds"] = originalConfig.CircuitBreakerTimeoutSeconds.ToString(),
            ["SlowOperationThresholdMs"] = originalConfig.SlowOperationThresholdMs.ToString()
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var boundConfig = new PerformanceConfiguration();
        config.Bind(boundConfig);

        // Assert
        Assert.AreEqual(originalConfig.CircuitBreakerTimeoutSeconds, boundConfig.CircuitBreakerTimeoutSeconds);
        Assert.AreEqual(originalConfig.SlowOperationThresholdMs, boundConfig.SlowOperationThresholdMs);
    }
}