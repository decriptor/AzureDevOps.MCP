namespace AzureDevOps.MCP.Authorization;

/// <summary>
/// Represents the result of an authorization check.
/// </summary>
public readonly record struct AuthorizationResult
{
	public bool IsAuthorized { get; init; }
	public string? ErrorMessage { get; init; }
	public Exception? Exception { get; init; }

	AuthorizationResult(bool isAuthorized, string? errorMessage = null, Exception? exception = null)
	{
		IsAuthorized = isAuthorized;
		ErrorMessage = errorMessage;
		Exception = exception;
	}

	public static AuthorizationResult Success() => new(true);

	public static AuthorizationResult Forbidden(string message) => new(false, message);

	public static AuthorizationResult Error(string message, Exception? exception = null) =>
		new(false, message, exception);

	public void ThrowIfNotAuthorized()
	{
		if (!IsAuthorized)
		{
			throw Exception ?? new UnauthorizedAccessException(ErrorMessage ?? "Access denied");
		}
	}
}