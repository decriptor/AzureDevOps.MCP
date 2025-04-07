namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Exception thrown when a rate limit is exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
	/// <summary>
	/// The rate limit status when the exception occurred.
	/// </summary>
	public RateLimitStatus Status { get; }

	/// <summary>
	/// Initializes a new instance of the RateLimitExceededException.
	/// </summary>
	/// <param name="status">The rate limit status</param>
	public RateLimitExceededException (RateLimitStatus status)
		: base ($"Rate limit exceeded. {status.RequestsRemaining} requests remaining in window.")
	{
		Status = status;
	}
}