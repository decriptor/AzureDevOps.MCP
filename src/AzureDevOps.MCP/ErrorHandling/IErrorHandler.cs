namespace AzureDevOps.MCP.ErrorHandling;

/// <summary>
/// Provides error handling capabilities for operations.
/// </summary>
public interface IErrorHandler
{
	/// <summary>
	/// Executes an operation with error handling and retry logic.
	/// </summary>
	/// <typeparam name="T">The return type of the operation</typeparam>
	/// <param name="operation">The operation to execute</param>
	/// <param name="operationName">The name of the operation for logging</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The result of the operation</returns>
	Task<T> ExecuteWithErrorHandlingAsync<T>(
		Func<CancellationToken, Task<T>> operation,
		string operationName,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an operation with error handling and retry logic.
	/// </summary>
	/// <param name="operation">The operation to execute</param>
	/// <param name="operationName">The name of the operation for logging</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task ExecuteWithErrorHandlingAsync(
		Func<CancellationToken, Task> operation,
		string operationName,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Determines if an exception should be retried.
	/// </summary>
	/// <param name="exception">The exception to check</param>
	/// <returns>True if the operation should be retried</returns>
	bool ShouldRetry(Exception exception);

	/// <summary>
	/// Gets the delay before the next retry attempt.
	/// </summary>
	/// <param name="attemptNumber">The current attempt number</param>
	/// <returns>The delay before retrying</returns>
	TimeSpan GetRetryDelay(int attemptNumber);
}