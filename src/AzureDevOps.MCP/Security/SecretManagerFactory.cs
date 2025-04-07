using AzureDevOps.MCP.Configuration;
using Microsoft.Extensions.Options;

namespace AzureDevOps.MCP.Security;

/// <summary>
/// Secret manager factory for dependency injection.
/// </summary>
public static class SecretManagerFactory
{
	/// <summary>
	/// Creates the appropriate secret manager based on configuration.
	/// </summary>
	/// <param name="serviceProvider">The service provider</param>
	/// <param name="config">The production configuration</param>
	/// <returns>The configured secret manager instance</returns>
	public static ISecretManager CreateSecretManager (
		IServiceProvider serviceProvider,
		IOptions<ProductionConfiguration> config)
	{
		var securityConfig = config.Value.Security;

		if (securityConfig.EnableKeyVault) {
			return serviceProvider.GetRequiredService<ProductionSecretManager> ();
		}

		return serviceProvider.GetRequiredService<EnvironmentSecretManager> ();
	}
}