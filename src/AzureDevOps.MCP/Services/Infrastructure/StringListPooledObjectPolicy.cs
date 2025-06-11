using Microsoft.Extensions.ObjectPool;

namespace AzureDevOps.MCP.Services.Infrastructure;

/// <summary>
/// Pooled object policy for List of strings to ensure proper cleanup.
/// </summary>
public class StringListPooledObjectPolicy : IPooledObjectPolicy<List<string>>
{
	/// <summary>
	/// Creates a new List of strings.
	/// </summary>
	/// <returns>A new List of strings</returns>
	public List<string> Create() => new();

	/// <summary>
	/// Prepares a List of strings for return to the pool.
	/// </summary>
	/// <param name="obj">The list to return</param>
	/// <returns>True if the list should be returned to the pool</returns>
	public bool Return(List<string> obj)
	{
		if (obj.Count > 100) // Don't pool very large lists
			return false;

		obj.Clear();
		return true;
	}
}