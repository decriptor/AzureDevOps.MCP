using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureDevOps.MCP.Services.Core;

/// <summary>
/// Service for managing Azure DevOps Git repositories.
/// Follows Single Responsibility Principle - only handles repository-related operations.
/// </summary>
public interface IRepositoryService
{
	/// <summary>
	/// Retrieves all Git repositories in a project.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of Git repositories</returns>
	Task<IEnumerable<GitRepository>> GetRepositoriesAsync (
		string projectName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves items (files and folders) from a repository path.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="path">The path to browse (empty for root)</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of Git items</returns>
	Task<IEnumerable<GitItem>> GetRepositoryItemsAsync (
		string projectName, string repositoryId, string path, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves the content of a specific file.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="path">The file path</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>The file content as a string</returns>
	Task<string> GetFileContentAsync (
		string projectName, string repositoryId, string path, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves commit history for a repository.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="branch">The branch name (optional)</param>
	/// <param name="limit">Maximum number of commits to return</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of Git commits</returns>
	Task<IEnumerable<GitCommitRef>> GetCommitsAsync (
		string projectName, string repositoryId, string? branch = null, int limit = 50,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves pull requests for a repository.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="status">The pull request status filter (optional)</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of pull requests</returns>
	Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync (
		string projectName, string repositoryId, string? status = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves branches for a repository.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of Git branches</returns>
	Task<IEnumerable<GitRef>> GetBranchesAsync (
		string projectName, string repositoryId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves tags for a repository.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>A collection of Git tags</returns>
	Task<IEnumerable<GitRef>> GetTagsAsync (
		string projectName, string repositoryId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a repository exists in the specified project.
	/// </summary>
	/// <param name="projectName">The name or ID of the project</param>
	/// <param name="repositoryId">The repository ID or name</param>
	/// <param name="cancellationToken">Cancellation token for the operation</param>
	/// <returns>True if the repository exists, false otherwise</returns>
	Task<bool> RepositoryExistsAsync (
		string projectName, string repositoryId, CancellationToken cancellationToken = default);
}