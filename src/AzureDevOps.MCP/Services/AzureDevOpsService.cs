using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
// Build, Test, Wiki, and Search APIs are not available as separate packages
// They are part of the main Azure DevOps packages but may require additional setup

namespace AzureDevOps.MCP.Services;

public class AzureDevOpsService : IAzureDevOpsService
{
	readonly IConfiguration _configuration;
	VssConnection? _connection;

	public AzureDevOpsService (IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task<VssConnection> GetConnectionAsync ()
	{
		if (_connection != null) {
			return _connection;
		}

		var orgUrl = _configuration["AzureDevOps:OrganizationUrl"];
		var pat = _configuration["AzureDevOps:PersonalAccessToken"]; if (string.IsNullOrEmpty (orgUrl) || string.IsNullOrEmpty (pat)) {
			throw new InvalidOperationException ("Azure DevOps organization URL and PAT must be configured");
		}

		var credentials = new VssBasicCredential (string.Empty, pat);
		_connection = new VssConnection (new Uri (orgUrl), credentials);
		await _connection.ConnectAsync ();
		return _connection;
	}

	public async Task<IEnumerable<TeamProjectReference>> GetProjectsAsync ()
	{
		var connection = await GetConnectionAsync ();
		var projectClient = connection.GetClient<ProjectHttpClient> ();
		return await projectClient.GetProjects ();
	}

	public async Task<IEnumerable<GitRepository>> GetRepositoriesAsync (string projectName)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> ();
		return await gitClient.GetRepositoriesAsync (projectName);
	}

	public async Task<IEnumerable<GitItem>> GetRepositoryItemsAsync (string projectName, string repositoryId, string path)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> (); var items = await gitClient.GetItemsAsync (
			repositoryId,
			recursionLevel: VersionControlRecursionType.OneLevel,
			scopePath: path);

		return items;
	}

	public async Task<string> GetFileContentAsync (string projectName, string repositoryId, string path)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> (); var item = await gitClient.GetItemAsync (
			repositoryId,
			path,
			includeContent: true);

		return item.Content;
	}

	public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync (string projectName, int limit = 100)
	{
		var connection = await GetConnectionAsync ();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient> ();

		// Create a WIQL query to get work items
		var wiql = new Wiql {
			Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{projectName}' ORDER BY [System.ChangedDate] DESC"
		};

		var result = await witClient.QueryByWiqlAsync (wiql);        // If no work items are found, return empty list
		if (!result.WorkItems.Any ()) {
			return [];
		}

		// Get the actual work items with fields
		var ids = result.WorkItems.Select (wi => wi.Id).Take (limit);
		return await witClient.GetWorkItemsAsync (ids, expand: WorkItemExpand.All);
	}

	public async Task<WorkItem?> GetWorkItemAsync (int id)
	{
		var connection = await GetConnectionAsync ();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient> ();

		try {
			return await witClient.GetWorkItemAsync (id, expand: WorkItemExpand.All);
		} catch (VssServiceException) {
			return null;
		}
	}

	// Git operations
	public async Task<IEnumerable<GitCommitRef>> GetCommitsAsync (string projectName, string repositoryId, string? branch = null, int limit = 50)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> ();
		var criteria = new GitQueryCommitsCriteria {
			Top = limit
		};
		if (!string.IsNullOrEmpty (branch)) {
			criteria.ItemVersion = new GitVersionDescriptor {
				Version = branch,
				VersionType = GitVersionType.Branch
			};
		}
		return await gitClient.GetCommitsAsync (repositoryId, criteria);
	}

	public async Task<IEnumerable<GitPullRequest>> GetPullRequestsAsync (string projectName, string repositoryId, string? status = null)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> ();
		var criteria = new GitPullRequestSearchCriteria {
			RepositoryId = Guid.Parse (repositoryId)
		};
		if (!string.IsNullOrEmpty (status) && Enum.TryParse<PullRequestStatus> (status, true, out var parsedStatus)) {
			criteria.Status = parsedStatus;
		}
		return await gitClient.GetPullRequestsAsync (projectName, repositoryId, criteria);
	}

	public async Task<IEnumerable<GitRef>> GetBranchesAsync (string projectName, string repositoryId)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> (); return await gitClient.GetRefsAsync (repositoryId, filter: "heads/");
	}

	public async Task<IEnumerable<GitRef>> GetTagsAsync (string projectName, string repositoryId)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> (); return await gitClient.GetRefsAsync (repositoryId, filter: "tags/");
	}

	public async Task<Microsoft.TeamFoundation.SourceControl.WebApi.Comment> AddPullRequestCommentAsync (string projectName, string repositoryId, int pullRequestId, string content, int? parentCommentId = null)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> ();

		var comment = new Microsoft.TeamFoundation.SourceControl.WebApi.Comment {
			Content = content,
			ParentCommentId = (short)(parentCommentId ?? 0),
			CommentType = CommentType.Text
		};

		var thread = new GitPullRequestCommentThread {
			Comments = new List<Microsoft.TeamFoundation.SourceControl.WebApi.Comment> { comment },
			Status = CommentThreadStatus.Active
		};

		var createdThread = await gitClient.CreateThreadAsync (thread, repositoryId, pullRequestId, projectName);
		return createdThread.Comments.First ();
	}

	public async Task<GitPullRequest> CreateDraftPullRequestAsync (string projectName, string repositoryId, string sourceBranch, string targetBranch, string title, string description)
	{
		var connection = await GetConnectionAsync ();
		var gitClient = connection.GetClient<GitHttpClient> ();

		var pullRequest = new GitPullRequest {
			SourceRefName = $"refs/heads/{sourceBranch}",
			TargetRefName = $"refs/heads/{targetBranch}",
			Title = title,
			Description = description,
			IsDraft = true
		};

		return await gitClient.CreatePullRequestAsync (pullRequest, repositoryId, projectName);
	}

	public async Task<WorkItem> UpdateWorkItemTagsAsync (int workItemId, string[] tagsToAdd, string[] tagsToRemove)
	{
		var connection = await GetConnectionAsync ();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient> ();

		// Get current work item to read existing tags
		var workItem = await witClient.GetWorkItemAsync (workItemId);
		if (workItem == null) {
			throw new InvalidOperationException ($"Work item {workItemId} not found");
		}

		// Get current tags
		var currentTags = new HashSet<string> ();
		if (workItem.Fields.ContainsKey ("System.Tags") && workItem.Fields["System.Tags"] is string tagsString && !string.IsNullOrEmpty (tagsString)) {
			currentTags = tagsString.Split (';', StringSplitOptions.RemoveEmptyEntries)
				.Select (t => t.Trim ())
				.ToHashSet (StringComparer.OrdinalIgnoreCase);
		}

		// Add new tags
		foreach (var tag in tagsToAdd) {
			if (!string.IsNullOrWhiteSpace (tag)) {
				currentTags.Add (tag.Trim ());
			}
		}

		// Remove specified tags
		foreach (var tag in tagsToRemove) {
			if (!string.IsNullOrWhiteSpace (tag)) {
				currentTags.Remove (tag.Trim ());
			}
		}

		// Create patch document
		var patchDocument = new JsonPatchDocument ();
		var newTagsString = string.Join ("; ", currentTags.OrderBy (t => t));

		patchDocument.Add (new JsonPatchOperation {
			Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
			Path = "/fields/System.Tags",
			Value = newTagsString
		});

		return await witClient.UpdateWorkItemAsync (patchDocument, workItemId);
	}

	// Search operations - Commented out due to missing SearchHttpClient
	/*
	public async Task<IEnumerable<CodeSearchResult>> SearchCodeAsync(string searchText, string? projectName = null, string? repositoryName = null, int limit = 50)
	{
		var connection = await GetConnectionAsync();
		var searchClient = await connection.GetClientAsync<SearchHttpClient>();
		
		var request = new CodeSearchRequest
		{
			SearchText = searchText,
			Filters = new Dictionary<string, IEnumerable<string>>()
		};
		
		if (!string.IsNullOrEmpty(projectName))
		{
			request.Filters["Project"] = new[] { projectName };
		}
		
		if (!string.IsNullOrEmpty(repositoryName))
		{
			request.Filters["Repository"] = new[] { repositoryName };
		}
		
		request.Top = limit;
		request.Skip = 0;
		
		var response = await searchClient.FetchCodeSearchResultsAsync(request);
		
		return response.Results.Select(r => new CodeSearchResult
		{
			FileName = r.FileName,
			FilePath = r.Path,
			Repository = r.Repository?.Name ?? string.Empty,
			Project = r.Project?.Name ?? string.Empty,
			Matches = r.Matches?.ToDictionary(m => m.CharOffset, m => m.Line) ?? new Dictionary<int, string>()
		});
	}
	*/

	// Wiki operations - Commented out due to missing WikiHttpClient
	/*
	public async Task<IEnumerable<WikiReference>> GetWikisAsync(string projectName)
	{
		var connection = await GetConnectionAsync();
		var wikiClient = await connection.GetClientAsync<WikiHttpClient>();
		
		var wikis = await wikiClient.GetAllWikisAsync(projectName);
		
		return wikis.Select(w => new WikiReference
		{
			Id = w.Id,
			Name = w.Name,
			Type = w.Type.ToString(),
			Url = w.RemoteUrl
		});
	}
	
	public async Task<WikiPage?> GetWikiPageAsync(string projectName, string wikiIdentifier, string path)
	{
		var connection = await GetConnectionAsync();
		var wikiClient = await connection.GetClientAsync<WikiHttpClient>();
		
		try
		{
			var page = await wikiClient.GetPageAsync(projectName, wikiIdentifier, path);
			var pageData = await wikiClient.GetPageTextAsync(projectName, wikiIdentifier, path);
			
			return new WikiPage
			{
				Path = page.Path,
				Content = pageData,
				Order = page.Order,
				IsParentPage = page.IsParentPage
			};
		}
		catch (VssServiceException)
		{
			return null;
		}
	}
	*/

	// Build and test operations - Commented out until proper Azure DevOps packages are available
	// The required API clients (BuildHttpClient, TestManagementHttpClient) are not available in the current packages
	/*
	public async Task<IEnumerable<Build>> GetBuildsAsync(string projectName, int? definitionId = null, int limit = 20)
	{
		var connection = await GetConnectionAsync();
		var buildClient = await connection.GetClientAsync<BuildHttpClient>();
		
		var definitions = definitionId.HasValue ? new[] { definitionId.Value } : null;
		
		return await buildClient.GetBuildsAsync(projectName, definitions: definitions, top: limit);
	}
	
	public async Task<IEnumerable<TestRun>> GetTestRunsAsync(string projectName, int limit = 20)
	{
		var connection = await GetConnectionAsync();
		var testClient = await connection.GetClientAsync<TestManagementHttpClient>();
		
		return await testClient.GetTestRunsAsync(projectName, top: limit);
	}
	
	public async Task<IEnumerable<TestCaseResult>> GetTestResultsAsync(string projectName, int runId)
	{
		var connection = await GetConnectionAsync();
		var testClient = await connection.GetClientAsync<TestManagementHttpClient>();
		
		return await testClient.GetTestResultsAsync(projectName, runId);
	}
	*/

	// Artifact operations
	// Commenting out DownloadBuildArtifactAsync as BuildHttpClient is not available in current packages
	/*
	public async Task<Stream> DownloadBuildArtifactAsync(string projectName, int buildId, string artifactName)
	{
		var connection = await GetConnectionAsync();
		var buildClient = await connection.GetClientAsync<BuildHttpClient>();
		
		var artifact = await buildClient.GetArtifactAsync(projectName, buildId, artifactName);
		
		if (artifact?.Resource?.DownloadUrl == null)
		{
			throw new InvalidOperationException($"Artifact '{artifactName}' not found or has no download URL");
		}
		
		var httpClient = new HttpClient();
		return await httpClient.GetStreamAsync(artifact.Resource.DownloadUrl);
	}
	*/
}
