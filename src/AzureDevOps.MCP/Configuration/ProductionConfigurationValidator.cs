using Microsoft.Extensions.Options;

namespace AzureDevOps.MCP.Configuration;

/// <summary>
/// Configuration validator for production settings.
/// </summary>
public class ProductionConfigurationValidator : IValidateOptions<ProductionConfiguration>
{
	/// <summary>
	/// Validates the production configuration.
	/// </summary>
	/// <param name="name">The name of the options instance</param>
	/// <param name="options">The options to validate</param>
	/// <returns>The validation result</returns>
	public ValidateOptionsResult Validate (string? name, ProductionConfiguration options)
	{
		var errors = options.ValidateConfiguration ();

		if (errors.Count != 0) {
			var errorMessage = string.Join ("; ", errors);
			return ValidateOptionsResult.Fail (errorMessage);
		}

		return ValidateOptionsResult.Success;
	}
}