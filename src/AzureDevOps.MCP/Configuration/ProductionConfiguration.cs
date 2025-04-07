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
	public AzureDevOpsConfiguration AzureDevOps { get; set; } = new () {
		OrganizationUrl = "",
		PersonalAccessToken = ""
	};

	/// <summary>
	/// Caching configuration for performance optimization.
	/// </summary>
	public CachingConfiguration Caching { get; set; } = new ();

	/// <summary>
	/// Security configuration for production environments.
	/// </summary>
	public ProductionSecurityConfiguration Security { get; set; } = new ();

	/// <summary>
	/// Performance and monitoring configuration.
	/// </summary>
	public PerformanceConfiguration Performance { get; set; } = new ();

	/// <summary>
	/// Logging configuration for observability.
	/// </summary>
	public LoggingConfiguration Logging { get; set; } = new ();

	/// <summary>
	/// Rate limiting configuration to prevent abuse.
	/// </summary>
	public RateLimitingConfiguration RateLimiting { get; set; } = new ();

	/// <summary>
	/// Health check configuration for monitoring.
	/// </summary>
	public HealthCheckConfiguration HealthChecks { get; set; } = new ();

	/// <summary>
	/// Environment-specific settings.
	/// </summary>
	[Required]
	public EnvironmentConfiguration Environment { get; set; } = new ();
}