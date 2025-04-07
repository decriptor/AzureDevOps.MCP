namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Configuration options for retry policy.
/// </summary>
public class RetryOptions
{
	/// <summary>
	/// Maximum number of retry attempts. Default is 3.
	/// </summary>
	public int MaxAttempts { get; set; } = 3;

	/// <summary>
	/// Base delay between retries. Default is 500ms.
	/// </summary>
	public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds (500);

	/// <summary>
	/// Maximum delay between retries. Default is 30 seconds.
	/// </summary>
	public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds (30);

	/// <summary>
	/// Multiplier for exponential backoff. Default is 2.0.
	/// </summary>
	public double BackoffMultiplier { get; set; } = 2.0;

	/// <summary>
	/// Function to determine if an exception should be retried. Default is DefaultShouldRetry.
	/// </summary>
	public Func<Exception, bool> ShouldRetry { get; set; } = DefaultShouldRetry;

	/// <summary>
	/// Default retry logic for common exceptions.
	/// </summary>
	/// <param name="exception">The exception to check</param>
	/// <returns>True if the operation should be retried</returns>
	static bool DefaultShouldRetry (Exception exception)
	{
		return exception switch {
			TaskCanceledException => false,
			OperationCanceledException => false,
			ArgumentException => false,
			InvalidOperationException => false,
			HttpRequestException => true,
			TimeoutException => true,
			_ => true
		};
	}
}