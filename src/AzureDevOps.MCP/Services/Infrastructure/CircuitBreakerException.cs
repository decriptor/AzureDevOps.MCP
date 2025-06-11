namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Exception thrown when a circuit breaker blocks a request.
/// </summary>
public class CircuitBreakerException : Exception
{
	/// <summary>
	/// The state of the circuit breaker when the exception occurred.
	/// </summary>
	public CircuitBreakerState State { get; }

	/// <summary>
	/// Initializes a new instance of the CircuitBreakerException.
	/// </summary>
	/// <param name="state">The circuit breaker state</param>
	/// <param name="message">The exception message</param>
	public CircuitBreakerException(CircuitBreakerState state, string message) : base(message)
	{
		State = state;
	}

	/// <summary>
	/// Initializes a new instance of the CircuitBreakerException.
	/// </summary>
	/// <param name="state">The circuit breaker state</param>
	/// <param name="message">The exception message</param>
	/// <param name="innerException">The inner exception</param>
	public CircuitBreakerException(CircuitBreakerState state, string message, Exception innerException) : base(message, innerException)
	{
		State = state;
	}
}