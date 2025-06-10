using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps build operations.
/// Follows Single Responsibility Principle - only handles build-related operations.
/// </summary>
public interface IBuildService
{
	/// <summary>
	/// Retrieves build definitions for a project.
	/// </summary>
	/// <param name="projectNameOrId">The project name or ID</param>
	/// <param name="limit">Maximum number of definitions to return</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of build definitions</returns>
	Task<IEnumerable<BuildDefinitionReference>> GetBuildDefinitionsAsync(string projectNameOrId, int limit = 100, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves a specific build definition.
	/// </summary>
	/// <param name="projectNameOrId">The project name or ID</param>
	/// <param name="definitionId">The build definition ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Build definition details or null if not found</returns>
	Task<BuildDefinition?> GetBuildDefinitionAsync(string projectNameOrId, int definitionId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves builds for a project.
	/// </summary>
	/// <param name="projectNameOrId">The project name or ID</param>
	/// <param name="definitionId">Optional build definition ID to filter by</param>
	/// <param name="limit">Maximum number of builds to return</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of builds</returns>
	Task<IEnumerable<Build>> GetBuildsAsync(string projectNameOrId, int? definitionId = null, int limit = 50, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves a specific build.
	/// </summary>
	/// <param name="projectNameOrId">The project name or ID</param>
	/// <param name="buildId">The build ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Build details or null if not found</returns>
	Task<Build?> GetBuildAsync(string projectNameOrId, int buildId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves agent pools available to the organization.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of agent pools</returns>
	Task<IEnumerable<TaskAgentPool>> GetAgentPoolsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves agents in a specific pool.
	/// </summary>
	/// <param name="poolId">The agent pool ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of agents</returns>
	Task<IEnumerable<TaskAgent>> GetAgentsAsync(int poolId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simplified build definition reference model.
/// </summary>
public class BuildDefinitionReference
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Path { get; set; } = string.Empty;
	public TeamProjectReference? Project { get; set; }
	public string QueueStatus { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Simplified build definition model.
/// </summary>
public class BuildDefinition
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Path { get; set; } = string.Empty;
	public TeamProjectReference? Project { get; set; }
	public string Description { get; set; } = string.Empty;
	public string Repository { get; set; } = string.Empty;
	public string DefaultBranch { get; set; } = string.Empty;
	public Dictionary<string, object> Variables { get; set; } = new();
	public List<BuildStep> Steps { get; set; } = new();
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Simplified build model.
/// </summary>
public class Build
{
	public int Id { get; set; }
	public string BuildNumber { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string Result { get; set; } = string.Empty;
	public BuildDefinitionReference? Definition { get; set; }
	public string SourceBranch { get; set; } = string.Empty;
	public string SourceVersion { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime? FinishTime { get; set; }
	public string RequestedBy { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Simplified build step model.
/// </summary>
public class BuildStep
{
	public string DisplayName { get; set; } = string.Empty;
	public string Task { get; set; } = string.Empty;
	public Dictionary<string, object> Inputs { get; set; } = new();
	public bool Enabled { get; set; } = true;
}

/// <summary>
/// Simplified task agent pool model.
/// </summary>
public class TaskAgentPool
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string PoolType { get; set; } = string.Empty;
	public int Size { get; set; }
	public bool IsHosted { get; set; }
	public DateTime CreatedOn { get; set; }
}

/// <summary>
/// Simplified task agent model.
/// </summary>
public class TaskAgent
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public bool Enabled { get; set; }
	public DateTime CreatedOn { get; set; }
	public string OperatingSystem { get; set; } = string.Empty;
}