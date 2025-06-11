namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Interface for retry policy implementations.
/// </summary>
public interface IRetryPolicy
{
	/// <summary>
	/// Executes an operation with retry logic.
	/// </summary>
	/// <typeparam name="T">The return type of the operation</typeparam>
	/// <param name="operation">The operation to execute</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The result of the operation</returns>
	Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an operation with retry logic.
	/// </summary>
	/// <param name="operation">The operation to execute</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}