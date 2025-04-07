namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Service for performing health checks on system components.
/// </summary>
public interface IHealthCheckService
{
	/// <summary>
	/// Performs a comprehensive health check.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The overall health status</returns>
	Task<HealthStatus> CheckHealthAsync (CancellationToken cancellationToken = default);

	/// <summary>
	/// Performs health checks on all registered components.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Health status for each component</returns>
	Task<Dictionary<string, HealthStatus>> CheckAllHealthAsync (CancellationToken cancellationToken = default);

	/// <summary>
	/// Registers a health check for a component.
	/// </summary>
	/// <param name="name">The name of the health check</param>
	/// <param name="check">The health check function</param>
	void RegisterCheck (string name, Func<CancellationToken, Task<HealthStatus>> check);

	/// <summary>
	/// Event fired when health status changes.
	/// </summary>
	event EventHandler<HealthChangedEventArgs>? HealthChanged;
}