namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Event arguments for health status changes.
/// </summary>
public class HealthChangedEventArgs : EventArgs
{
	/// <summary>
	/// The name of the health check that changed.
	/// </summary>
	public string CheckName { get; }

	/// <summary>
	/// The new health status.
	/// </summary>
	public HealthStatus Status { get; }

	/// <summary>
	/// The previous health status, if available.
	/// </summary>
	public HealthStatus? PreviousStatus { get; }

	/// <summary>
	/// Initializes a new instance of the HealthChangedEventArgs.
	/// </summary>
	/// <param name="checkName">The name of the health check</param>
	/// <param name="status">The new health status</param>
	/// <param name="previousStatus">The previous health status</param>
	public HealthChangedEventArgs(string checkName, HealthStatus status, HealthStatus? previousStatus = null)
	{
		CheckName = checkName;
		Status = status;
		PreviousStatus = previousStatus;
	}
}