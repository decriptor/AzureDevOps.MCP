using System.Security.Claims;

namespace AzureDevOps.MCP.Authorization;

/// <summary>
/// Provides authorization services for Azure DevOps MCP operations.
/// </summary>
public interface IAuthorizationService
{
	/// <summary>
	/// Authorizes a user to perform an operation on a resource.
	/// </summary>
	/// <param name="user">The user to authorize</param>
	/// <param name="resource">The resource being accessed</param>
	/// <param name="operation">The operation being performed</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Authorization result</returns>
	Task<AuthorizationResult> AuthorizeAsync(
		ClaimsPrincipal user,
		string resource,
		string operation,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Authorizes a user to access a project with specific permissions.
	/// </summary>
	/// <param name="user">The user to authorize</param>
	/// <param name="projectName">The project name</param>
	/// <param name="permission">The required permission</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Authorization result</returns>
	Task<AuthorizationResult> AuthorizeProjectAccessAsync(
		ClaimsPrincipal user,
		string projectName,
		ProjectPermission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a user has a specific permission.
	/// </summary>
	/// <param name="user">The user to check</param>
	/// <param name="permission">The permission to check</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if the user has the permission</returns>
	Task<bool> HasPermissionAsync(
		ClaimsPrincipal user,
		string permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if an operation can be performed (simplified method for decorator usage).
	/// </summary>
	/// <param name="operation">The operation to check</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if the operation can be performed</returns>
	Task<bool> CanPerformOperationAsync(string operation, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a project can be accessed (simplified method for decorator usage).
	/// </summary>
	/// <param name="projectNameOrId">The project name or ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if the project can be accessed</returns>
	Task<bool> CanAccessProjectAsync(string projectNameOrId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a repository can be accessed (simplified method for decorator usage).
	/// </summary>
	/// <param name="projectNameOrId">The project name or ID</param>
	/// <param name="repositoryId">The repository ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if the repository can be accessed</returns>
	Task<bool> CanAccessRepositoryAsync(string projectNameOrId, string repositoryId, CancellationToken cancellationToken = default);
}