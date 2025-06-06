using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps projects.
/// Follows Single Responsibility Principle - only handles project-related operations.
/// </summary>
public interface IProjectService
{
	/// <summary>
	/// Retrieves all projects accessible to the authenticated user.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>
	/// A collection of <see cref="TeamProjectReference"/> objects representing
	/// the projects. Returns empty collection if no projects are accessible.
	/// </returns>
	/// <exception cref="UnauthorizedAccessException">
	/// Thrown when the user lacks permission to access projects.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the Azure DevOps service is unavailable.
	/// </exception>
	Task<IEnumerable<TeamProjectReference>> GetProjectsAsync (CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves detailed information about a specific project.
	/// </summary>
	/// <param name="projectNameOrId">The name or ID of the project</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>
	/// A <see cref="TeamProject"/> object if found, null otherwise.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown when projectNameOrId is null, empty, or invalid.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">
	/// Thrown when the user lacks permission to access the specific project.
	/// </exception>
	Task<TeamProject?> GetProjectAsync (string projectNameOrId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves project properties for a specific project.
	/// </summary>
	/// <param name="projectNameOrId">The name or ID of the project</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>
	/// A collection of project properties as key-value pairs.
	/// </returns>
	Task<IEnumerable<ProjectProperty>> GetProjectPropertiesAsync (string projectNameOrId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a project exists and is accessible to the current user.
	/// </summary>
	/// <param name="projectNameOrId">The name or ID of the project</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>True if the project exists and is accessible, false otherwise.</returns>
	Task<bool> ProjectExistsAsync (string projectNameOrId, CancellationToken cancellationToken = default);
}
