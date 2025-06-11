using AzureDevOps.MCP.Services.Core;
using AzureDevOps.MCP.Services.Infrastructure;
using AzureDevOps.MCP.Services;
using AzureDevOps.MCP.ErrorHandling;
using AzureDevOps.MCP.Configuration;
using AzureDevOps.MCP.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzureDevOps.MCP.Extensions;

/// <summary>
/// Extension methods for setting up Azure DevOps MCP services in an IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds all Azure DevOps MCP services to the container with production configuration.
	/// </summary>
	public static IServiceCollection AddAzureDevOpsMcpServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Add production configuration
		services.AddProductionConfiguration(configuration);

		// Add infrastructure services
		services.AddInfrastructureServices();

		// Add core services
		services.AddCoreServices();

		// Add production services
		services.AddProductionServices();

		return services;
	}

	/// <summary>
	/// Configures production settings with comprehensive validation.
	/// </summary>
	public static IServiceCollection AddProductionConfiguration(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Register main production configuration
		services.Configure<ProductionConfiguration>(configuration);
		
		// Register legacy Azure DevOps configuration for backward compatibility
		services.Configure<AzureDevOpsConfiguration>(
			configuration.GetSection("AzureDevOps"));

		// Add configuration validation
		services.AddSingleton<IValidateOptions<AzureDevOpsConfiguration>, AzureDevOpsConfigurationValidator>();
		services.AddSingleton<IValidateOptions<ProductionConfiguration>, ProductionConfigurationValidator>();

		return services;
	}

	/// <summary>
	/// Configures Azure DevOps settings and validates configuration (legacy method).
	/// </summary>
	public static IServiceCollection AddAzureDevOpsConfiguration(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Register configuration with validation
		services.Configure<AzureDevOpsConfiguration>(
			configuration.GetSection("AzureDevOps"));

		// Add configuration validation
		services.AddSingleton<IValidateOptions<AzureDevOpsConfiguration>, AzureDevOpsConfigurationValidator>();

		return services;
	}

	/// <summary>
	/// Adds infrastructure services (simplified for core functionality).
	/// </summary>
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
	{
		// Connection management
		services.AddSingleton<IAzureDevOpsConnectionFactory, AzureDevOpsConnectionFactory>();
		
		// Memory optimization infrastructure
		services.AddSingleton<Microsoft.Extensions.ObjectPool.ObjectPoolProvider, Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider>();
		services.AddSingleton<MemoryPoolService>();
		
		// Basic caching
		services.AddSingleton<ICacheService, CacheService>();
		services.AddSingleton<MemoryOptimizedCacheService>();
		
		// Error handling  
		services.AddScoped<IErrorHandler, ResilientErrorHandler>();
		
		// Performance tracking
		services.AddSingleton<IPerformanceService, PerformanceService>();
		services.AddSingleton<ISentryPerformanceService, SentryPerformanceService>();
		
		// Audit service
		services.AddScoped<IAuditService, AuditService>();
		
		// Authorization service
		services.AddScoped<Authorization.IAuthorizationService, Authorization.BasicAuthorizationService>();
		
		// Security/Secret management (will be overridden by production services if enabled)
		services.AddSingleton<Security.EnvironmentSecretManager>();
		services.AddSingleton<Security.ProductionSecretManager>();
		services.AddSingleton<Security.ISecretManager>(provider =>
		{
			var config = provider.GetRequiredService<IOptions<ProductionConfiguration>>();
			return SecretManagerFactory.CreateSecretManager(provider, config);
		});

		return services;
	}

	/// <summary>
	/// Adds production-ready services for monitoring and observability.
	/// </summary>
	public static IServiceCollection AddProductionServices(this IServiceCollection services)
	{
		// Add memory caching with production configuration
		services.AddMemoryCache(options =>
		{
			// Configure memory cache based on production settings
			var serviceProvider = services.BuildServiceProvider();
			var config = serviceProvider.GetService<IOptions<ProductionConfiguration>>();
			if (config?.Value?.Caching != null)
			{
				options.SizeLimit = config.Value.Caching.MaxMemoryCacheSizeMB * 1024 * 1024; // Convert MB to bytes
			}
		});

		// Add basic health checks
		services.AddHealthChecks();

		// Add logging configuration
		services.AddLogging(builder =>
		{
			// Configure structured logging if enabled
			var serviceProvider = services.BuildServiceProvider();
			var config = serviceProvider.GetService<IOptions<ProductionConfiguration>>();
			if (config?.Value?.Logging?.EnableStructuredLogging == true)
			{
				// Additional logging configuration can be added here
			}
		});

		return services;
	}

	/// <summary>
	/// Adds core domain services.
	/// </summary>
	public static IServiceCollection AddCoreServices(this IServiceCollection services)
	{
		// Core domain services - focused on working functionality
		services.AddScoped<IProjectService, ProjectService>();
		services.AddScoped<IRepositoryService, RepositoryService>();
		services.AddScoped<IWorkItemService, WorkItemService>();
		services.AddScoped<IBuildService, BuildService>();
		services.AddScoped<ITestService, TestService>();
		services.AddScoped<ISearchService, SearchService>();
		
		// Tool registrations
		services.AddScoped<Tools.AzureDevOpsTools>();
		services.AddScoped<Tools.BatchTools>();
		services.AddScoped<Tools.PerformanceTools>();
		services.AddScoped<Tools.SafeWriteTools>();
		services.AddScoped<Tools.TestPlanTools>();

		return services;
	}
}