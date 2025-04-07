namespace AzureDevOps.MCP.Validation;

public class ValidationResult
{
	public bool IsValid { get; init; }
	public string? ErrorMessage { get; init; }

	ValidationResult () { }

	public static ValidationResult Valid () => new () { IsValid = true };

	public static ValidationResult Invalid (string errorMessage) => new () {
		IsValid = false,
		ErrorMessage = errorMessage ?? "Validation failed"
	};

	public void ThrowIfInvalid ()
	{
		if (!IsValid) {
			throw new ValidationException (ErrorMessage ?? "Validation failed");
		}
	}
}