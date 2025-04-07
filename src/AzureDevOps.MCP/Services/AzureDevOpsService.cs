using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

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
			Comments = [comment],
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

	public async Task<WorkItemComment> AddWorkItemCommentAsync (string projectName, int workItemId, string content)
	{
		var connection = await GetConnectionAsync ();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient> ();

		// Create a comment using work item tracking client
		// Work item comments are typically added through work item history
		var patchDocument = new JsonPatchDocument {
			new JsonPatchOperation {
				Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
				Path = "/fields/System.History",
				Value = content
			}
		};

		var updatedWorkItem = await witClient.UpdateWorkItemAsync (patchDocument, workItemId);

		// Return a WorkItemComment representation
		return new WorkItemComment {
			Id = updatedWorkItem.Rev ?? 0, // Use revision as comment ID
			Text = content,
			CreatedDate = DateTime.UtcNow,
			CreatedBy = "MCP Server",
			WorkItemId = workItemId
		};
	}

	public async Task<WorkItem> UpdateWorkItemTagsAsync (int workItemId, string[] tagsToAdd, string[] tagsToRemove)
	{
		var connection = await GetConnectionAsync ();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient> ();

		// Get current work item to read existing tags
		var workItem = await witClient.GetWorkItemAsync (workItemId) ?? throw new InvalidOperationException ($"Work item {workItemId} not found");

		// Get current tags
		var currentTags = new HashSet<string> ();
		if (workItem.Fields.TryGetValue ("System.Tags", out var value) && value is string tagsString && !string.IsNullOrEmpty (tagsString)) {
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

	public async Task<WorkItem> CreateWorkItemAsync (string projectName, string workItemType, string title, string description, string? tags = null)
	{
		var connection = await GetConnectionAsync ();
		var witClient = connection.GetClient<WorkItemTrackingHttpClient> ();

		// Create patch document for new work item
		var patchDocument = new JsonPatchDocument {
			new JsonPatchOperation {
				Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
				Path = "/fields/System.Title",
				Value = title
			},
			new JsonPatchOperation {
				Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
				Path = "/fields/System.Description",
				Value = description
			}
		};

		// Add tags if provided
		if (!string.IsNullOrEmpty (tags)) {
			patchDocument.Add (new JsonPatchOperation {
				Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
				Path = "/fields/System.Tags",
				Value = tags
			});
		}

		return await witClient.CreateWorkItemAsync (patchDocument, projectName, workItemType);
	}

}
