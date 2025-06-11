namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Represents the health status of a component.
/// </summary>
public class HealthStatus
{
	/// <summary>
	/// Whether the component is healthy.
	/// </summary>
	public bool IsHealthy { get; init; }

	/// <summary>
	/// Optional description of the health status.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Additional data about the health status.
	/// </summary>
	public Dictionary<string, object>? Data { get; init; }

	/// <summary>
	/// The time it took to perform the health check.
	/// </summary>
	public TimeSpan? ResponseTime { get; init; }

	/// <summary>
	/// Any exception that occurred during the health check.
	/// </summary>
	public Exception? Exception { get; init; }

	/// <summary>
	/// Creates a healthy status.
	/// </summary>
	/// <param name="description">Optional description</param>
	/// <param name="data">Optional additional data</param>
	/// <param name="responseTime">Optional response time</param>
	/// <returns>A healthy status</returns>
	public static HealthStatus Healthy(string? description = null, Dictionary<string, object>? data = null, TimeSpan? responseTime = null)
		=> new() { IsHealthy = true, Description = description, Data = data, ResponseTime = responseTime };

	/// <summary>
	/// Creates an unhealthy status.
	/// </summary>
	/// <param name="description">Optional description</param>
	/// <param name="exception">Optional exception</param>
	/// <param name="data">Optional additional data</param>
	/// <param name="responseTime">Optional response time</param>
	/// <returns>An unhealthy status</returns>
	public static HealthStatus Unhealthy(string? description = null, Exception? exception = null, Dictionary<string, object>? data = null, TimeSpan? responseTime = null)
		=> new() { IsHealthy = false, Description = description, Exception = exception, Data = data, ResponseTime = responseTime };
}