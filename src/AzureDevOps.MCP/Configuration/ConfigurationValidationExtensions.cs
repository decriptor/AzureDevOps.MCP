namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Configuration validation extensions for production readiness.
/// </summary>
public static class ConfigurationValidationExtensions
{
	/// <summary>
	/// Validates the production configuration and returns validation results.
	/// </summary>
	public static List<string> ValidateConfiguration (this ProductionConfiguration config)
	{
		var errors = new List<string> ();
		// Azure DevOps validation
		if (string.IsNullOrWhiteSpace (config.AzureDevOps.OrganizationUrl)) {
			errors.Add ("Azure DevOps Organization URL is required");
		}

		if (string.IsNullOrWhiteSpace (config.AzureDevOps.PersonalAccessToken)) {
			errors.Add ("Azure DevOps Personal Access Token is required");
		}

		if (!string.IsNullOrWhiteSpace (config.AzureDevOps.OrganizationUrl) &&
			!Uri.TryCreate (config.AzureDevOps.OrganizationUrl, UriKind.Absolute, out _)) {
			errors.Add ("Azure DevOps Organization URL must be a valid absolute URL");
		}

		// Security validation
		if (config.Security.EnableKeyVault && string.IsNullOrWhiteSpace (config.Security.KeyVaultUrl)) {
			errors.Add ("Key Vault URL is required when Key Vault is enabled");
		}

		if (config.Security.EnableApiKeyAuth && config.Security.ApiKeyHashes.Count == 0) {
			errors.Add ("At least one API key hash is required when API key authentication is enabled");
		}

		// Caching validation
		if (config.Caching.EnableDistributedCache && string.IsNullOrWhiteSpace (config.Caching.RedisConnectionString)) {
			errors.Add ("Redis connection string is required when distributed caching is enabled");
		}

		// Environment validation
		if (string.IsNullOrWhiteSpace (config.Environment.Name)) {
			errors.Add ("Environment name is required");
		}

		var validEnvironments = new[] { "Development", "Staging", "Production" };
		if (!validEnvironments.Contains (config.Environment.Name)) {
			errors.Add ($"Environment name must be one of: {string.Join (", ", validEnvironments)}");
		}

		return errors;
	}

	/// <summary>
	/// Throws an exception if the configuration is invalid.
	/// </summary>
	public static void ThrowIfInvalid (this ProductionConfiguration config)
	{
		var errors = config.ValidateConfiguration ();
		if (errors.Count != 0) {
			throw new InvalidOperationException ($"Configuration validation failed:\n{string.Join ("\n", errors)}");
		}
	}
}