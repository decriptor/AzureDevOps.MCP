using System.ComponentModel.DataAnnotations;
using AzureDevOps.MCP.Services.Infrastructure;

namespace AzureDevOps.MCP.Configuration;

public class ImprovedAzureDevOpsConfiguration
{
	[Required]
	[Url]
	public required string OrganizationUrl { get; set; }

	[Required]
	[MinLength (10)]
	public required string PersonalAccessToken { get; set; }

	public List<string> EnabledWriteOperations { get; set; } = new ();
	public bool RequireConfirmation { get; set; } = true;
	public bool EnableAuditLogging { get; set; } = true;

	public MonitoringConfiguration Monitoring { get; set; } = new ();
	public CacheConfiguration Cache { get; set; } = new ();
	public ResilienceConfiguration Resilience { get; set; } = new ();
	public SecurityConfiguration Security { get; set; } = new ();
}

public class CacheConfiguration
{
	public bool Enabled { get; set; } = true;
	public int MaxSizeMB { get; set; } = 100;
	public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes (5);
	public bool EnableMemoryPressureManagement { get; set; } = true;
	public long MemoryPressureThresholdMB { get; set; } = 1000;
}

public class ResilienceConfiguration
{
	public CircuitBreakerOptions CircuitBreaker { get; set; } = new ();
	public RetryOptions Retry { get; set; } = new ();
	public RateLimitOptions RateLimit { get; set; } = new ();
	public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes (2);
}

public class CircuitBreakerOptions
{
	public bool Enabled { get; set; } = true;
	public int FailureThreshold { get; set; } = 5;
	public TimeSpan OpenDuration { get; set; } = TimeSpan.FromMinutes (1);
}

public class SecurityConfiguration
{
	public bool EnableInputValidation { get; set; } = true;
	public bool EnableAuditLogging { get; set; } = true;
	public int MaxAuditLogEntries { get; set; } = 1000;
	public TimeSpan TokenValidationCacheExpiry { get; set; } = TimeSpan.FromMinutes (30);
}

public class ConfigurationValidator
{
	public static ValidationResult ValidateConfiguration (ImprovedAzureDevOpsConfiguration config)
	{
		var errors = new List<string> ();

		// Validate organization URL
		if (!Uri.TryCreate (config.OrganizationUrl, UriKind.Absolute, out var uri) ||
			(!uri.Scheme.Equals ("https", StringComparison.OrdinalIgnoreCase) &&
			 !uri.Scheme.Equals ("http", StringComparison.OrdinalIgnoreCase))) {
			errors.Add ("OrganizationUrl must be a valid HTTP/HTTPS URL");
		}

		// Validate PAT (basic check - not exposing in logs)
		if (string.IsNullOrWhiteSpace (config.PersonalAccessToken) || config.PersonalAccessToken.Length < 10) {
			errors.Add ("PersonalAccessToken must be at least 10 characters long");
		}

		// Validate write operations
		var invalidOperations = config.EnabledWriteOperations
			.Where (op => !SafeWriteOperations.AllOperations.Contains (op))
			.ToList ();

		if (invalidOperations.Any ()) {
			errors.Add ($"Invalid write operations: {string.Join (", ", invalidOperations)}");
		}

		// Validate cache configuration
		if (config.Cache.MaxSizeMB <= 0) {
			errors.Add ("Cache.MaxSizeMB must be positive");
		}

		if (config.Cache.DefaultExpiration <= TimeSpan.Zero) {
			errors.Add ("Cache.DefaultExpiration must be positive");
		}

		// Validate resilience configuration
		if (config.Resilience.CircuitBreaker.FailureThreshold <= 0) {
			errors.Add ("CircuitBreaker.FailureThreshold must be positive");
		}

		if (config.Resilience.Retry.MaxAttempts <= 0) {
			errors.Add ("Retry.MaxAttempts must be positive");
		}

		if (config.Resilience.RateLimit.RequestsPerWindow <= 0) {
			errors.Add ("RateLimit.RequestsPerWindow must be positive");
		}

		return errors.Any ()
			? new ValidationResult (string.Join ("; ", errors))
			: ValidationResult.Success ?? new ValidationResult (null);
	}
}