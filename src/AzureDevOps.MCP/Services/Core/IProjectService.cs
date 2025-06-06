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
    Task<IEnumerable<TeamProjectReference>> GetProjectsAsync(CancellationToken cancellationToken = default);

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
    Task<TeamProject?> GetProjectAsync(string projectNameOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves project properties for a specific project.
    /// </summary>
    /// <param name="projectNameOrId">The name or ID of the project</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// A collection of project properties as key-value pairs.
    /// </returns>
    Task<IEnumerable<ProjectProperty>> GetProjectPropertiesAsync(string projectNameOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project exists and is accessible to the current user.
    /// </summary>
    /// <param name="projectNameOrId">The name or ID of the project</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>True if the project exists and is accessible, false otherwise.</returns>
    Task<bool> ProjectExistsAsync(string projectNameOrId, CancellationToken cancellationToken = default);
}

public interface IRepositoryService
{
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitRepository>> GetRepositoriesAsync(
        string projectName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitItem>> GetRepositoryItemsAsync(
        string projectName, string repositoryId, string path, CancellationToken cancellationToken = default);
    Task<string> GetFileContentAsync(
        string projectName, string repositoryId, string path, CancellationToken cancellationToken = default);
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitCommitRef>> GetCommitsAsync(
        string projectName, string repositoryId, string? branch = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitPullRequest>> GetPullRequestsAsync(
        string projectName, string repositoryId, string? status = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitRef>> GetBranchesAsync(
        string projectName, string repositoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Microsoft.TeamFoundation.SourceControl.WebApi.GitRef>> GetTagsAsync(
        string projectName, string repositoryId, CancellationToken cancellationToken = default);
}

public interface IWorkItemService
{
    Task<IEnumerable<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>> GetWorkItemsAsync(
        string projectName, int limit = 100, CancellationToken cancellationToken = default);
    Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem?> GetWorkItemAsync(
        int id, CancellationToken cancellationToken = default);
}

public interface IBuildService
{
    Task<IEnumerable<Microsoft.TeamFoundation.Build.WebApi.Build>> GetBuildsAsync(
        string projectName, int? definitionId = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<Stream> DownloadBuildArtifactAsync(
        string projectName, int buildId, string artifactName, CancellationToken cancellationToken = default);
}

public interface ITestService
{
    Task<IEnumerable<Microsoft.TeamFoundation.TestManagement.WebApi.TestRun>> GetTestRunsAsync(
        string projectName, int limit = 20, CancellationToken cancellationToken = default);
    Task<IEnumerable<Microsoft.TeamFoundation.TestManagement.WebApi.TestCaseResult>> GetTestResultsAsync(
        string projectName, int runId, CancellationToken cancellationToken = default);
}

public interface ISearchService
{
    Task<IEnumerable<CodeSearchResult>> SearchCodeAsync(
        string searchText, string? projectName = null, string? repositoryName = null, 
        int limit = 50, CancellationToken cancellationToken = default);
}

public interface IWikiService
{
    Task<IEnumerable<WikiReference>> GetWikisAsync(
        string projectName, CancellationToken cancellationToken = default);
    Task<WikiPage?> GetWikiPageAsync(
        string projectName, string wikiIdentifier, string path, CancellationToken cancellationToken = default);
}

public interface IWriteOperationService
{
    Task<Microsoft.TeamFoundation.SourceControl.WebApi.Comment> AddPullRequestCommentAsync(
        string projectName, string repositoryId, int pullRequestId, string content, 
        int? parentCommentId = null, CancellationToken cancellationToken = default);
    Task<Microsoft.TeamFoundation.SourceControl.WebApi.GitPullRequest> CreateDraftPullRequestAsync(
        string projectName, string repositoryId, string sourceBranch, string targetBranch, 
        string title, string description, CancellationToken cancellationToken = default);
    Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> UpdateWorkItemTagsAsync(
        int workItemId, string[] tagsToAdd, string[] tagsToRemove, CancellationToken cancellationToken = default);
}