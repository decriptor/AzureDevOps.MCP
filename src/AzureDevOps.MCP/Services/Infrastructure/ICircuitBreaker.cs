namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Interface for circuit breaker pattern implementation.
/// </summary>
public interface ICircuitBreaker
{
	/// <summary>
	/// Executes an operation through the circuit breaker.
	/// </summary>
	/// <typeparam name="T">The return type of the operation</typeparam>
	/// <param name="operation">The operation to execute</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The result of the operation</returns>
	/// <exception cref="CircuitBreakerException">Thrown when the circuit is open</exception>
	Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes an operation through the circuit breaker.
	/// </summary>
	/// <param name="operation">The operation to execute</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <exception cref="CircuitBreakerException">Thrown when the circuit is open</exception>
	Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

	/// <summary>
	/// The current state of the circuit breaker.
	/// </summary>
	CircuitBreakerState State { get; }

	/// <summary>
	/// The number of consecutive failures.
	/// </summary>
	int FailureCount { get; }

	/// <summary>
	/// The time of the last failure, if any.
	/// </summary>
	DateTime? LastFailureTime { get; }

	/// <summary>
	/// Resets the circuit breaker to closed state.
	/// </summary>
	void Reset();
}