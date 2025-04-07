namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Represents the current state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
	/// <summary>
	/// Circuit is closed - requests are allowed through.
	/// </summary>
	Closed,

	/// <summary>
	/// Circuit is open - requests are blocked due to failures.
	/// </summary>
	Open,

	/// <summary>
	/// Circuit is half-open - testing if the service has recovered.
	/// </summary>
	HalfOpen
}