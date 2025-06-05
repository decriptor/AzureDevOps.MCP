using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.MCP.Services;

public class AzureDevOpsService : IAzureDevOpsService
{
	private readonly IConfiguration _configuration;
	private VssConnection? _connection;

	public AzureDevOpsService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task<VssConnection> GetConnectionAsync()
	{        if (_connection != null)
        {
            return _connection;
        }

		var orgUrl = _configuration["AzureDevOps:OrganizationUrl"];
		var pat = _configuration["AzureDevOps:PersonalAccessToken"];        if (string.IsNullOrEmpty(orgUrl) || string.IsNullOrEmpty(pat))
        {
            throw new InvalidOperationException("Azure DevOps organization URL and PAT must be configured");
        }

		var credentials = new VssBasicCredential(string.Empty, pat);
		_connection = new VssConnection(new Uri(orgUrl), credentials);
		await _connection.ConnectAsync();
		return _connection;
	}

	public async Task<IEnumerable<TeamProjectReference>> GetProjectsAsync()
	{
		var connection = await GetConnectionAsync();
		var projectClient = connection.GetClient<ProjectHttpClient>();
		return await projectClient.GetProjects();
	}

	public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync(string projectName)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();
		return await gitClient.GetRepositoriesAsync(projectName);
	}

	public async Task<IEnumerable<GitItem>> GetRepositoryItemsAsync(string projectName, string repositoryId, string path)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();        var items = await gitClient.GetItemsAsync(
            repositoryId,
            recursionLevel: VersionControlRecursionType.OneLevel,
            scopePath: path);

		return items;
	}

	public async Task<string> GetFileContentAsync(string projectName, string repositoryId, string path)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();        var item = await gitClient.GetItemAsync(
            repositoryId,
            path,
            includeContent: true);

		return item.Content;
	}

	public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(string projectName, int limit = 100)
	{
		var connection = await GetConnectionAsync();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

		// Create a WIQL query to get work items
		var wiql = new Wiql
		{
			Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{projectName}' ORDER BY [System.ChangedDate] DESC"
		};

		var result = await witClient.QueryByWiqlAsync(wiql);        // If no work items are found, return empty list
        if (!result.WorkItems.Any())
        {
            return [];
        }

		// Get the actual work items with fields
		var ids = result.WorkItems.Select(wi => wi.Id).Take(limit);
		return await witClient.GetWorkItemsAsync(ids, expand: WorkItemExpand.All);
	}

	public async Task<WorkItem?> GetWorkItemAsync(int id)
	{
		var connection = await GetConnectionAsync();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

		try
		{
			return await witClient.GetWorkItemAsync(id, expand: WorkItemExpand.All);
		}
		catch (VssServiceException)
		{
			return null;
		}
	}

	// Git operations
	public async Task<IEnumerable<GitCommitRef>> GetCommitsAsync(string projectName, string repositoryId, string? branch = null, int limit = 50)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();
		var criteria = new GitQueryCommitsCriteria
		{
			Top = limit
		};
		if (!string.IsNullOrEmpty(branch))
		{
			criteria.ItemVersion = new GitVersionDescriptor
			{
				Version = branch,
				VersionType = GitVersionType.Branch
			};
		}        return await gitClient.GetCommitsAsync(repositoryId, criteria);
	}

	public async Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync(string projectName, string repositoryId, string? status = null)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();
		var criteria = new GitPullRequestSearchCriteria
		{
			RepositoryId = Guid.Parse(repositoryId)
		};
		if (!string.IsNullOrEmpty(status) && Enum.TryParse<PullRequestStatus>(status, true, out var parsedStatus))
		{
			criteria.Status = parsedStatus;
		}
		return await gitClient.GetPullRequestsAsync(projectName, repositoryId, criteria);
	}

	public async Task<IEnumerable<GitRef>> GetBranchesAsync(string projectName, string repositoryId)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();        return await gitClient.GetRefsAsync(repositoryId, filter: "heads/");
	}

	public async Task<IEnumerable<GitRef>> GetTagsAsync(string projectName, string repositoryId)
	{
		var connection = await GetConnectionAsync();
		var gitClient = connection.GetClient<GitHttpClient>();        return await gitClient.GetRefsAsync(repositoryId, filter: "tags/");
	}
}
