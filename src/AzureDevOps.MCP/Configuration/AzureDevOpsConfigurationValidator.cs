using Microsoft.Extensions.Options;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Configuration validator for Azure DevOps settings.
/// </summary>
public class AzureDevOpsConfigurationValidator : IValidateOptions<AzureDevOpsConfiguration>
{
	/// <summary>
	/// Validates the Azure DevOps configuration.
	/// </summary>
	/// <param name="name">The name of the options instance</param>
	/// <param name="options">The options to validate</param>
	/// <returns>The validation result</returns>
	public ValidateOptionsResult Validate (string? name, AzureDevOpsConfiguration options)
	{
		if (string.IsNullOrEmpty (options.OrganizationUrl)) {
			return ValidateOptionsResult.Fail ("OrganizationUrl is required");
		}

		if (string.IsNullOrEmpty (options.PersonalAccessToken)) {
			return ValidateOptionsResult.Fail ("PersonalAccessToken is required");
		}

		if (!Uri.TryCreate (options.OrganizationUrl, UriKind.Absolute, out _)) {
			return ValidateOptionsResult.Fail ("OrganizationUrl must be a valid URL");
		}

		return ValidateOptionsResult.Success;
	}
}