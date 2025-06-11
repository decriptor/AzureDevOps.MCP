namespace AzureDevOps.MCP.ErrorHandling;

/// <summary>
/// Categorizes different types of errors for proper handling.
/// </summary>
public enum ErrorCategory
{
	/// <summary>
	/// Unknown or uncategorized error.
	/// </summary>
	Unknown,

	/// <summary>
	/// Client-side error (e.g., invalid request).
	/// </summary>
	ClientError,

	/// <summary>
	/// Authentication failed.
	/// </summary>
	AuthenticationError,

	/// <summary>
	/// Authorization failed (insufficient permissions).
	/// </summary>
	AuthorizationError,

	/// <summary>
	/// Requested resource not found.
	/// </summary>
	NotFound,

	/// <summary>
	/// Rate limit exceeded.
	/// </summary>
	RateLimited,

	/// <summary>
	/// Server-side error.
	/// </summary>
	ServerError,

	/// <summary>
	/// Network connectivity issue.
	/// </summary>
	NetworkError,

	/// <summary>
	/// Operation timed out.
	/// </summary>
	Timeout
}