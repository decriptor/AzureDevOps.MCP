namespace AzureDevOps.MCP.ErrorHandling;

/// <summary>
/// Provides context information for operations being executed.
/// </summary>
public class OperationContext
{
	/// <summary>
	/// The name of the operation being executed.
	/// </summary>
	public required string OperationName { get; init; }

	/// <summary>
	/// Additional properties associated with the operation.
	/// </summary>
	public Dictionary<string, object> Properties { get; init; } = [];

	/// <summary>
	/// The time when the operation started.
	/// </summary>
	public DateTime StartTime { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// The ID of the user executing the operation.
	/// </summary>
	public string? UserId { get; init; }

	/// <summary>
	/// A correlation ID for tracking the operation across services.
	/// </summary>
	public string? CorrelationId { get; init; }
}